"""Label and assemble Unity-rendered boss preview frames; no pose synthesis."""
from pathlib import Path
import argparse
from PIL import Image,ImageDraw,ImageFont

def main():
 p=argparse.ArgumentParser();p.add_argument('directory',type=Path);a=p.parse_args();root=a.directory
 ids=['EclipseWraith','ThornRegent','FrostMourner','CrimsonHarvester','AbyssOracle']
 names=['Ảnh Hồn Nhật Thực','Cổ Vương Gai','Ai Nữ Băng Sương','Huyết Nguyệt Đao Phủ','Tiên Tri Vực Thẳm'];colors=['#64dfab','#d5b85b','#8ddcf5','#ec6374','#bd8ff2']
 smooth=(root/'EclipseWraith-384.png').exists();native=(root/'EclipseWraith-153.png').exists();count=385 if smooth else 154 if native else 48
 font=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',17);small=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',15)
 frames=[]
 for f in range(count):
  canvas=Image.new('RGB',(1280,338),'#071013');d=ImageDraw.Draw(canvas)
  if native:
   t=f/(50 if smooth else 20);state='CHỜ' if t<1.6 else 'DI CHUYỂN' if t<4 else 'CHUẨN BỊ' if t<5.1 else 'TUNG ĐÒN' if t<6 else 'NHẬN ĐÒN' if t<6.36 else 'CHẾT'
  else:state='CHỜ' if f<8 else 'DI CHUYỂN' if f<16 else 'CHUẨN BỊ' if f<24 else 'TUNG ĐÒN' if f<32 else 'NHẬN ĐÒN' if f<36 else 'CHẾT'
  for i,boss in enumerate(ids):
   name=f'{boss}-{f:03}.png' if native else f'{boss}-{f:02}.png'
   im=Image.open(root/name).convert('RGB').resize((256,256),Image.Resampling.LANCZOS);canvas.paste(im,(i*256,40));d.text((i*256+128,15),names[i],font=font,fill=colors[i],anchor='mm');d.text((i*256+128,309),state,font=small,fill='#e1d4ad',anchor='mm')
  frames.append(canvas)
  if f%40==0:print('assembled',f,flush=True)
 palette=frames[0].quantize(colors=256)
 encoded=[f.quantize(palette=palette,dither=Image.Dither.FLOYDSTEINBERG) for f in frames]
 output=root/('boss-unique-native-animation.gif' if native else 'boss-animation-preview-v2.gif')
 encoded[0].save(output,save_all=True,append_images=encoded[1:],duration=20 if smooth else 50 if native else 150,loop=0,optimize=False,disposal=2)
 poses=[0,112,230,267,310,345] if smooth else [0,45,92,107,124,138] if native else [0,23,24]
 sheet=Image.new('RGB',(1280,338*len(poses)),'#071013')
 for r,n in enumerate(poses):sheet.paste(frames[n],(0,r*338))
 sheet.save(root/'boss-native-key-poses.png' if native else root/'boss-animation-key-poses-v2.png');print(output,flush=True)
if __name__=='__main__':main()

