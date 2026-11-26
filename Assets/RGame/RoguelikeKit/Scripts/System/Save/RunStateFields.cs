using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Object=UnityEngine.Object;
namespace RGame.RoguelikeKit
{
    [Serializable] public class RunAtom
    {
        public string value;public Vector4 vector,size;
    }
    [Serializable] public sealed class RunField:RunAtom
    {
        public string name,declaring;
        public List<RunAtom> items;
    }
    // Only fields present on the supplied, trusted runtime type may be restored.
    // A save never supplies a type to instantiate or a method to invoke.
    internal sealed class RunStateFields
    {
        readonly Dictionary<Object,string> ids=new();
        readonly Dictionary<string,Object> objects=new();
        const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.DeclaredOnly;
        public void Bind(string id,Object obj){if(obj==null)return;ids[obj]=id;objects[id]=obj;}
        public Object Resolve(string id)=>id!=null&&objects.TryGetValue(id,out var value)?value:null;
        public string Id(Object value)=>value!=null&&ids.TryGetValue(value,out var id)?id:null;
        public void BindAssets()
        {
            foreach(var asset in Resources.FindObjectsOfTypeAll<ScriptableObject>().OrderBy(x=>x.GetInstanceID()))
                if(asset is not global::RGame.CommonStat.CommonStatRuntimeSO&&asset is not SkillDataSO&&asset is not global::RGame.Framework.FloatWrapper&&!asset.name.EndsWith("(Clone)")&&!asset.hideFlags.HasFlag(HideFlags.DontSave))
                    Bind("asset:"+asset.GetType().FullName+":"+asset.name,asset);
        }
        // Deliberately not used as an asset identity: mutable per-enemy stats are
        // bound by their entity id by the checkpoint owner.
        static IEnumerable<FieldInfo> Fields(Type type)
        {
            for(var t=type;t!=null&&t!=typeof(MonoBehaviour)&&t!=typeof(ScriptableObject)&&t!=typeof(Object);t=t.BaseType)
                foreach(var field in t.GetFields(Flags))if(!field.IsStatic&&!typeof(Delegate).IsAssignableFrom(field.FieldType)&&!(field.DeclaringType==typeof(LevelManager)&&field.Name=="initializedPlayer"))yield return field;
        }
        static bool Atomic(Type t)=>t.IsEnum||t==typeof(bool)||t==typeof(int)||t==typeof(float)||t==typeof(double)||t==typeof(string)||t==typeof(Vector2)||t==typeof(Vector3)||t==typeof(Vector4)||t==typeof(Quaternion)||t==typeof(Color)||t==typeof(Bounds)||typeof(Object).IsAssignableFrom(t);
        public List<RunField> Capture(object source)
        {
            var result=new List<RunField>();
            foreach(var field in Fields(source.GetType()))
            {
                // Authored object references come from the trusted prefab/scene.
                // Persist runtime links, not asset identities based on display names.
                Type element=field.FieldType.IsArray?field.FieldType.GetElementType():field.FieldType;
                if(typeof(Object).IsAssignableFrom(element)&&(field.IsPublic||field.IsDefined(typeof(SerializeField),true)))continue;
                var node=Encode(field.FieldType,field.GetValue(source));if(node==null)continue;
                node.name=field.Name;node.declaring=field.DeclaringType.FullName;
                if(field.FieldType==typeof(float)&&node.value!=null)
                {
                    float value=(float)field.GetValue(source);
                    if(field.Name=="castStarted"||field.Name=="_attackLockUntil")node.value=(value-Time.time).ToString("R",CultureInfo.InvariantCulture);
                    if(field.Name=="_nextHitFlashAt")node.value=(value-Time.unscaledTime).ToString("R",CultureInfo.InvariantCulture);
                }
                result.Add(node);
            }
            return result;
        }
        RunField Encode(Type type,object value)
        {
            if(typeof(Object).IsAssignableFrom(type))return value is Object obj&&obj!=null&&ids.TryGetValue(obj,out var id)?new RunField{value=id}:null;
            if(type.IsArray&&Atomic(type.GetElementType()))
            {
                if(value is not Array array)return null;var node=new RunField{items=new List<RunAtom>()};
                foreach(var item in array){var entry=Encode(type.GetElementType(),item);if(entry==null)return null;node.items.Add(entry);}return node;
            }
            if(type.IsGenericType&&type.GetGenericTypeDefinition()==typeof(List<>)&&Atomic(type.GetGenericArguments()[0]))
            {
                if(value is not IList list)return null;var node=new RunField{items=new List<RunAtom>()};
                foreach(var item in list){var entry=Encode(type.GetGenericArguments()[0],item);if(entry!=null)node.items.Add(entry);}return node;
            }
            if(!Atomic(type))return null;
            var result=new RunField();
            if(type==typeof(Vector2)){var v=(Vector2)value;result.vector=new Vector4(v.x,v.y,0,0);}
            else if(type==typeof(Vector3)){var v=(Vector3)value;result.vector=new Vector4(v.x,v.y,v.z,0);}
            else if(type==typeof(Vector4))result.vector=(Vector4)value;
            else if(type==typeof(Quaternion)){var v=(Quaternion)value;result.vector=new Vector4(v.x,v.y,v.z,v.w);}
            else if(type==typeof(Color)){var v=(Color)value;result.vector=new Vector4(v.r,v.g,v.b,v.a);}
            else if(type==typeof(Bounds)){var v=(Bounds)value;result.vector=new Vector4(v.center.x,v.center.y,v.center.z,0);result.size=new Vector4(v.size.x,v.size.y,v.size.z,0);}
            else result.value=type.IsEnum?Convert.ToInt32(value).ToString(CultureInfo.InvariantCulture):Convert.ToString(value,CultureInfo.InvariantCulture);
            return result;
        }
        public void Restore(object target,List<RunField> values)
        {
            if(values==null||values.Count>256)throw new ArgumentException("Invalid checkpoint fields");
            var allowed=Fields(target.GetType()).ToArray();
            foreach(var node in values)
            {
                var field=allowed.SingleOrDefault(f=>f.Name==node.name&&f.DeclaringType.FullName==node.declaring);
                if(field==null)throw new ArgumentException("Checkpoint field no longer exists: "+node.name);
                var value=Decode(field.FieldType,node);
                if(value is float number)
                {
                    if(field.Name=="castStarted"||field.Name=="_attackLockUntil")value=number+Time.time;
                    if(field.Name=="_nextHitFlashAt")value=number+Time.unscaledTime;
                }
                if(!field.IsInitOnly)field.SetValue(target,value);
                else if(field.GetValue(target) is Array array&&value is Array saved)
                {if(array.Length!=saved.Length)throw new ArgumentException("Checkpoint array size changed");Array.Copy(saved,array,array.Length);}
                else if(field.GetValue(target) is IList list&&value is IList items){list.Clear();foreach(var item in items)list.Add(item);}
            }
        }
        object Decode(Type type,RunAtom node)
        {
            if(typeof(Object).IsAssignableFrom(type))
            {var obj=Resolve(node.value);if(obj==null||!type.IsInstanceOfType(obj))throw new ArgumentException("Missing checkpoint reference: "+node.value);return obj;}
            if(type.IsArray&&Atomic(type.GetElementType()))
            {
                var values=((RunField)node).items;if(values==null||values.Count>2048)throw new ArgumentException("Invalid checkpoint array");
                var array=Array.CreateInstance(type.GetElementType(),values.Count);for(int i=0;i<array.Length;i++)array.SetValue(Decode(type.GetElementType(),values[i]),i);return array;
            }
            if(type.IsGenericType&&type.GetGenericTypeDefinition()==typeof(List<>)&&Atomic(type.GetGenericArguments()[0]))
            {
                var values=((RunField)node).items;if(values==null||values.Count>2048)throw new ArgumentException("Invalid checkpoint list");
                var list=(IList)Activator.CreateInstance(type);foreach(var entry in values)list.Add(Decode(type.GetGenericArguments()[0],entry));return list;
            }
            if(type==typeof(Vector2))return new Vector2(node.vector.x,node.vector.y);
            if(type==typeof(Vector3))return new Vector3(node.vector.x,node.vector.y,node.vector.z);
            if(type==typeof(Vector4))return node.vector;
            if(type==typeof(Quaternion))return new Quaternion(node.vector.x,node.vector.y,node.vector.z,node.vector.w);
            if(type==typeof(Color))return new Color(node.vector.x,node.vector.y,node.vector.z,node.vector.w);
            if(type==typeof(Bounds))return new Bounds(new Vector3(node.vector.x,node.vector.y,node.vector.z),new Vector3(node.size.x,node.size.y,node.size.z));
            if(type.IsEnum)return Enum.ToObject(type,int.Parse(node.value,CultureInfo.InvariantCulture));
            if(!Atomic(type))throw new ArgumentException("Unsupported checkpoint value");
            return Convert.ChangeType(node.value,type,CultureInfo.InvariantCulture);
        }
    }
}
