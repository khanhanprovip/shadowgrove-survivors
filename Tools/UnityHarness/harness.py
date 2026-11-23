"""Local Unity operations. No shell interpolation, cloud account or paid backend."""
import argparse
import contextlib
import datetime as dt
import hashlib
import json
import os
from pathlib import Path
import re
import subprocess
import sys
import time
import urllib.request

HERE = Path(__file__).resolve().parent
PROJECT = HERE.parent.parent
LOCK = json.loads((HERE / 'toolchain.lock.json').read_text())
CLI = HERE / '.tools' / 'unity' / LOCK['cliVersion'] / 'unity.exe'
SUITES = ['menus', 'backgrounds', 'skills', 'ui', 'survival', 'killtarget', 'endless',
          'animations', 'hit-feedback', 'skill-lifetime', 'audio', 'settings-write', 'settings-read', 'art', 'combat-pickups']
LEGACY = {'Smoke45': 'playtest_smoke45.txt', 'Flow65': 'playtest_flow65.txt',
          'Full100': 'playtest_full100.txt', 'Lose60': 'playtest_lose60.txt',
          'KillTargetLevel': 'playtest_killtarget_lv2.txt'}


def sha(path):
    with Path(path).open('rb') as stream:
        digest = hashlib.sha256()
        for chunk in iter(lambda: stream.read(1024 * 1024), b''): digest.update(chunk)
        return digest.hexdigest()


def write_json(path, value):
    Path(path).write_text(json.dumps(value, ensure_ascii=False, indent=2), encoding='utf-8')


def editor():
    version = re.search(r'm_EditorVersion:\s*(\S+)',
                        (PROJECT / 'ProjectSettings/ProjectVersion.txt').read_text()).group(1)
    root = Path(os.environ.get('UNITY_HARNESS_EDITOR_ROOT', LOCK['editorRoot']))
    path = root / version / 'Editor/Unity.exe'
    if not path.is_file():
        raise RuntimeError(f'Exact Editor {version} missing: {path}. Set UNITY_HARNESS_EDITOR_ROOT.')
    return path, version


def invoke(args, output, timeout, extra_env=None):
    env = os.environ.copy()
    env.update(extra_env or {})
    output.parent.mkdir(parents=True, exist_ok=True)
    started = time.monotonic()
    result = {'args': [str(x) for x in args], 'timeoutSeconds': timeout, 'timedOut': False}
    with output.open('wb') as stream:
        with subprocess.Popen(result['args'], cwd=PROJECT, env=env, stdout=stream,
                              stderr=subprocess.STDOUT,
                              creationflags=getattr(subprocess, 'CREATE_NO_WINDOW', 0)) as proc:
            try:
                result['exitCode'] = proc.wait(timeout=timeout)
            except subprocess.TimeoutExpired:
                # Only terminate the process created by this invocation.
                proc.kill()
                proc.wait()
                result.update(exitCode=124, timedOut=True)
    result['seconds'] = round(time.monotonic() - started, 2)
    result['log'] = str(output)
    write_json(output.with_suffix(output.suffix + '.process.json'), result)
    return result


def cli(args, run, label, timeout=180):
    split = args.index('--') if '--' in args else len(args)
    result = invoke([CLI, *args[:split], '--project-path', PROJECT, '--format', 'json',
                     '--non-interactive', *args[split:]], run / (label + '.json'), timeout)
    if result['exitCode']:
        raise RuntimeError(f'Unity CLI failed ({result["exitCode"]}): {result["log"]}')
    raw = Path(result['log']).read_text(encoding='utf-8-sig').strip()
    envelope = json.loads(raw) if raw else None  # Void Pipeline commands emit no payload.
    if isinstance(envelope, dict) and (envelope.get('success') is False or envelope.get('errors')):
        raise RuntimeError(f'Unity CLI reported failure: {result["log"]}')
    return envelope


def command(args, run, label, timeout=180):
    return cli(['command', '--caller', 'plugin', '--skill', 'unity-cli', *args], run, label, timeout)


def assess(report, runtime_log, process, legacy=False):
    """Fail closed: assertions alone never prove a healthy run."""
    reasons = []
    passes = len(re.findall(r'^PASS\b', report, re.M))
    fails = len(re.findall(r'^FAIL\b', report, re.M))
    pattern = r'== SO LOI/EXCEPTION: (\d+) ==' if legacy else r'^Runtime errors:\s*(\d+)\s*$'
    counts = re.findall(pattern, report, re.M)
    if not report: reasons.append('missing report')
    if not passes: reasons.append('no PASS assertions')
    if fails: reasons.append(f'{fails} failed assertions')
    if len(counts) != 1: reasons.append('missing/ambiguous runtime error summary')
    elif int(counts[0]): reasons.append(f'{counts[0]} runtime errors')
    if process['exitCode'] != 0: reasons.append(f'exit {process["exitCode"]}')
    if process['timedOut']: reasons.append('timeout')
    # Runtime collector starts after bootstrap; also inspect raw Player/Editor log.
    errors = re.findall(r'^.*(?:\b[A-Za-z]+Exception:|error CS\d+|Fatal error|Crash!!!).*$',
                        runtime_log, re.M)
    if errors: reasons.append(f'{len(errors)} error lines in raw runtime log')
    return {'passed': not reasons, 'passCount': passes, 'failCount': fails,
            'reasons': reasons, 'runtimeErrorLines': errors[:30]}


@contextlib.contextmanager
def operation_lock():
    import msvcrt
    with (HERE / '.operation.lock').open('a+b') as stream:
        if os.fstat(stream.fileno()).st_size == 0: stream.write(b'0'); stream.flush()
        stream.seek(0)
        try: msvcrt.locking(stream.fileno(), msvcrt.LK_NBLCK, 1)
        except OSError: raise RuntimeError('Another harness operation is active for this project.')
        try: yield
        finally:
            stream.seek(0)
            msvcrt.locking(stream.fileno(), msvcrt.LK_UNLCK, 1)


def require_closed_editor():
    # Unity holds this file exclusively. Opening it is read-only and does not remove stale files.
    path = PROJECT / 'Temp/UnityLockfile'
    if path.exists():
        try:
            with path.open('rb'): pass
        except OSError:
            raise RuntimeError('Project is open in Unity. Close it before a cold build/editor test.')


def read(path):
    return Path(path).read_text(encoding='utf-8-sig', errors='replace') if Path(path).is_file() else ''


def provenance(run):
    invoke(['git', '-c', 'core.fsmonitor=false', 'status', '--short'], run / 'git-status.txt', 30)
    invoke(['git', '-c', 'core.fsmonitor=false', 'rev-parse', 'HEAD'], run / 'git-head.txt', 30)
    # Includes uncommitted source, assets and local skills; excludes ignored build/cache files.
    tracked = subprocess.run(['git', '-c', 'core.fsmonitor=false', 'ls-files', '-co',
                              '--exclude-standard', '-z'], cwd=PROJECT, capture_output=True, check=True)
    files = {}
    for name in sorted(set(tracked.stdout.decode('utf-8').split('\0')) - {''}):
        path = PROJECT / name
        if path.is_file(): files[name] = sha(path)
    write_json(run / 'source-sha256.json', files)
    return files


@contextlib.contextmanager
def preserve_generated_deletions():
    # Addressables recreates these during a build. Preserve an existing deletion.
    paths = [PROJECT / ('Assets/RGame/RoguelikeKit/AddressableAssetsData/link.xml' + suffix)
             for suffix in ('', '.meta')]
    absent = [path for path in paths if not path.exists()]
    try: yield
    finally:
        for path in absent:
            if path.is_file(): path.unlink()


def probe(run, timeout):
    """Own one short-lived Editor; never close or repurpose the user's open Editor."""
    require_closed_editor()
    exe, _ = editor()
    with (run / 'editor-stdout.log').open('wb') as stream:
        with subprocess.Popen([str(exe), '-batchmode', '-projectPath', str(PROJECT), '-logFile',
                               str(run / 'editor.log')], cwd=PROJECT, stdout=stream,
                              stderr=subprocess.STDOUT,
                              creationflags=getattr(subprocess, 'CREATE_NO_WINDOW', 0)) as proc:
            playing = False
            ready = False
            try:
                cli(['status', '--until-ready', '--timeout', str(timeout)], run, 'ready', timeout + 10)
                ready = True
                cursor = command(['console', 'status'], run, 'console-before')['cursor']
                scene = LOCK['initializationScene']
                command(['script', 'eval', f'return UnityEditor.SceneManagement.EditorSceneManager.OpenScene("{scene}").name;'], run, 'open-initialization')
                authoring = command(['script', 'run', '--file', 'Tools/UnityHarness/VerifyAuthoring.cs',
                                     '--entry', 'VerifyAuthoring.Run'], run, 'authoring')
                if not authoring.get('result', '').startswith('PASS:'): raise RuntimeError('Authoring proof missing')
                command(['editor', 'set_auto_tick'], run, 'auto-tick-on')
                command(['editor.playmode', 'set_run_in_background'], run, 'background-on')
                command(['editor.playmode', 'start'], run, 'play-start')
                playing = True
                cli(['status', '--until-ready', '--timeout', str(timeout)], run, 'play-ready', timeout + 10)
                frames = command(['job', 'wait', '--', '--condition',
                                  json.dumps({'member': 'UnityEngine.Time.frameCount', 'op': 'changed'}),
                                  '--timeout', '10'], run, 'frame-proof')
                if not frames.get('met') or frames.get('framesObserved', 0) <= 0:
                    raise RuntimeError('Game frame counter did not advance')
                # Wait for async Initialization -> menu load, independently of frame proof.
                menu = command(['job', 'wait', '--', '--condition', json.dumps({
                    'member': 'UnityEngine.SceneManagement.SceneManager.sceneCount',
                    'op': 'greaterThan', 'value': 1}), '--timeout', '30'], run, 'scene-load')
                if not menu.get('met'): raise RuntimeError('Initialization did not load another scene')
                state = command(['script', 'eval',
                                 'return UnityEngine.Object.FindFirstObjectByType<RGame.RoguelikeKit.UIMainMenu>() != null;'], run, 'menu-proof')
                if state is not True: raise RuntimeError('Main menu component not found after Initialization')
                command(['editor.playmode', 'stop'], run, 'play-stop')
                playing = False
                cli(['status', '--until-ready', '--timeout', str(timeout)], run, 'stop-ready', timeout + 10)
                console = command(['console', 'status'], run, 'console-after')
                command(['console', 'get', '--', '--since', str(cursor), '--level', 'error'], run, 'console-errors')
                if console.get('counts', {}).get('error', 0) or console.get('groundTruth', {}).get('compilationFailed'):
                    raise RuntimeError('Editor console contains errors; inspect console-errors.json')
                return {'authoring': authoring, 'frames': frames, 'consoleErrors': 0}
            finally:
                try:
                    if ready and proc.poll() is None:
                        if playing: command(['editor.playmode', 'stop'], run, 'cleanup-stop')
                        command(['editor', 'set_auto_tick', '--', '--enabled', 'false'], run, 'cleanup-auto-tick')
                        command(['editor.playmode', 'set_run_in_background', '--', '--enabled', 'false'], run, 'cleanup-background')
                        command(['editor', 'quit'], run, 'editor-quit')
                        proc.wait(timeout=30)
                finally:
                    if proc.poll() is None:
                        proc.kill()
                        proc.wait()


def build(run, timeout):
    require_closed_editor()
    exe, _ = editor()
    target = run / 'Windows-x64'
    with preserve_generated_deletions():
        result = invoke([exe, '-batchmode', '-nographics', '-projectPath', PROJECT,
                         '-executeMethod', 'DATN.DatnBuild.BuildWindows64',
                         '-logFile', run / 'build.log'], run / 'build-stdout.log', timeout,
                        {'DATN_BUILD_OUTPUT_DIR': str(target)})
    binary = target / 'ShadowgroveSurvivors.exe'
    log = read(run / 'build.log')
    if result['exitCode'] or not binary.is_file() or not re.search(r'Ket qua: Succeeded.*errors: 0', log):
        raise RuntimeError(f'Build failed. See {run / "build.log"}')
    write_json(run / 'build.json', {'exe': str(binary), 'sha256': sha(binary),
                                   'assemblySha256': sha(target / 'ShadowgroveSurvivors_Data/Managed/Assembly-CSharp.dll')})
    return binary


def test_player(binary, suites, run, width, height, timeout):
    results = []
    for index, suite in enumerate(suites):
        case = run / f'{index + 1:02d}-{suite}-{width}x{height}'
        case.mkdir()
        # Each case isolated; settings-write/read intentionally share only their own profile.
        profile = run / (f'{run.name}-settings' if suite.startswith('settings-') else f'{run.name}-{index}-{suite}')
        proc = invoke([binary, '-datn-autotest', f'-datn-suite={suite}', '-screen-width', str(width),
                       '-screen-height', str(height), '-screen-fullscreen', '0', '-force-d3d11',
                       '-logFile', case / 'player.log'], case / 'stdout.log', timeout,
                      {'DATN_TEST_OUTPUT_DIR': str(case), 'DATN_TEST_PROFILE_DIR': str(profile)})
        result = assess(read(case / f'report-{suite}.txt'), read(case / 'player.log'), proc)
        result.update(suite=suite, output=str(case), process=proc)
        write_json(case / 'result.json', result)
        results.append(result)
        print(f'{suite}: {"PASS" if result["passed"] else "FAIL"} ({result["passCount"]} assertions)', flush=True)
        write_json(run / 'results.json', results)
    return all(x['passed'] for x in results)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('action', choices=['setup', 'doctor', 'live', 'probe', 'build', 'test', 'verify', 'legacy'])
    parser.add_argument('--exe', type=Path, help='Existing Windows build; test action only')
    parser.add_argument('--suites', default='ui,skills,skill-lifetime,audio')
    parser.add_argument('--width', type=int, default=1280)
    parser.add_argument('--height', type=int, default=720)
    parser.add_argument('--timeout', type=int, default=300, help='Per test/CLI timeout seconds')
    parser.add_argument('--build-timeout', type=int, default=1800)
    parser.add_argument('--case', choices=LEGACY, default='Smoke45')
    argv = sys.argv[1:]
    split = argv.index('--') if '--' in argv else len(argv)
    args, remainder = parser.parse_known_args(argv[:split])
    args.command = remainder + argv[split + 1:]
    if args.command and args.action != 'live': parser.error('Pipeline arguments require live action')
    run = HERE / 'artifacts' / (dt.datetime.now().strftime('%Y%m%d-%H%M%S') + '-' + args.action + '-' + str(os.getpid()))
    run.mkdir(parents=True)
    print(f'Artifacts: {run}', flush=True)
    outcome = {'action': args.action, 'passed': False, 'artifacts': str(run)}
    try:
        with operation_lock():
            exe, version = editor()
            outcome['editorVersion'] = version
            if args.action == 'setup':
                CLI.parent.mkdir(parents=True, exist_ok=True)
                if not CLI.exists():
                    pending = CLI.with_suffix('.download')
                    urllib.request.urlretrieve(LOCK['cliUrl'], pending)
                    if sha(pending) != LOCK['cliSha256']: raise RuntimeError('CLI checksum mismatch')
                    pending.replace(CLI)
            if not CLI.is_file() or sha(CLI) != LOCK['cliSha256']:
                raise RuntimeError('CLI missing/checksum mismatch. Run setup.')
            pipeline = json.loads((PROJECT / 'Packages/manifest.json').read_text())['dependencies'].get('com.unity.pipeline')
            if pipeline != LOCK['pipelineVersion']: raise RuntimeError(f'Expected Pipeline {LOCK["pipelineVersion"]}, got {pipeline}')
            if args.action in ('setup', 'doctor'):
                result = invoke([CLI, '--version'], run / 'cli-version.txt', 30)
                if result['exitCode']: raise RuntimeError('CLI version probe failed')
                outcome.update(editor=str(exe), cli=str(CLI), pipeline=pipeline, passed=True)
            elif args.action == 'live':
                if not args.command: raise RuntimeError('Use live -- <Pipeline command>')
                cli(['status', '--until-ready', '--timeout', str(args.timeout)], run, 'ready', args.timeout + 10)
                command(args.command, run, 'command', args.timeout)
                outcome['passed'] = True
            elif args.action == 'probe':
                outcome['proof'] = probe(run, args.timeout)
                outcome['passed'] = True
            else:
                if args.action == 'test' and not args.exe: raise RuntimeError('test requires --exe')
                suites = SUITES if args.suites == 'all' else args.suites.split(',')
                if not suites or any(x not in SUITES for x in suites): raise RuntimeError(f'Allowed suites: {SUITES}')
                provenance(run)
                if args.action == 'legacy':
                    require_closed_editor()
                    proc = invoke([exe, '-batchmode', '-projectPath', PROJECT, '-executeMethod',
                                   f'DATN.PlayTestRunner.{args.case}', '-logFile', run / 'editor.log'],
                                  run / 'stdout.log', args.timeout,
                                  {'DATN_TEST_OUTPUT_DIR': str(run), 'DATN_TEST_PROFILE_DIR': str(run / 'profile')})
                    result = assess(read(run / LEGACY[args.case]), read(run / 'editor.log'), proc, legacy=True)
                    write_json(run / 'result.json', result)
                    outcome['passed'] = result['passed']
                else:
                    binary = build(run, args.build_timeout) if args.action in ('build', 'verify') else args.exe.resolve()
                    if not binary.is_file(): raise RuntimeError(f'Build not found: {binary}')
                    outcome.update(exe=str(binary), exeSha256=sha(binary))
                    outcome['passed'] = (True if args.action == 'build' else
                                         test_player(binary, suites, run, args.width, args.height, args.timeout))
    except Exception as error:
        outcome['error'] = str(error)
        print(str(error), file=sys.stderr, flush=True)
    finally:
        write_json(run / 'summary.json', outcome)
    return 0 if outcome['passed'] else 1


if __name__ == '__main__':
    sys.exit(main())
