This tool is able to extract and modify the iso of the game Kingdom Hearts 2(Final Mix)
It uses a list that gives all of the ISO resource names in clear (msg/jp/al.bar) instead of their hashes(0x56203d96).
This list isn't complete yet but contains all the files needed for the translation.

This tool can launch more than one patch and mix them instead to have to apply them one by one.
Simply drag 'n drop all patches you need to apply to the toolkit or, with the 
windows cmd, execute the command(in the directory of KH2FM_Toolkit): KH2FM_Toolkit patch1.kh2patch patch2.kh2patch

You can change the name of the iso to modify simply by drag 'n drop him to the software or to use the command: "KH2FM_Toolkit [youroptions] YOURISO.ISO

Options:

[-help]: Extract this Readme
[-extractor]: Launch the extractor. Instead of patching the game, the toolkit will extract him
[-exit]: Just stop the soft. Nothing else(Making the "return;" action")
[-batch]: Skipping all the "Console.Readline();"(when you need to press enter)and closing automatically the soft at the end.
[-patchmaker]: Launching the patchmaker.

Changelog:
[2.5.0.0]
*Xeeynamo's patch format support added(patchmaker and patcher side)
*Patchmaker added
*Minor bug corrections
[2.3.0.4]
*Initial release


Patchmaker Options(to put after the option -patchmaker):

[-xeeynamo]: Using the encryption xeeynamo used WARNING: DESTRUCTIVE METHOD
[-batch]: Skipping all the "Console.Readline();"(when you need to press enter)and closing automatically the soft at the end. (Yes, another time)
[-version x]: Set the version to x. Need to be a entire number between 0 and 9
[-author x]: Set the author to x
[-changelog x]: Set the changelog to x
[-credits x]: Set the credits to x
[-skipchangelog]: Nothing is used for the changelogs, Changelog option is not shown at patching process
[-skipcredits]: Nothing is used for the credits, Changelog option is not shown at patching process
[-output something.kh2patch]: Set the output file to something.kh2patch

You can, for patch files, write 0, 1 or 2 to parent compressed file or KH2, OVL or ISO. I think it is enough obvious for don't have to explain this.

When you want to write the patch file, just leave blank a filename, it will create him


Thanks to xeeynamo that programmed his tools on C and for translated the game on english. Love you <3




-----Copyright 02/25/2014 ? GovanifY

                                                                                                                                                
                                                                                                                                                
        GGGGGGGGGGGGG                                                                           iiii     ffffffffffffffff  YYYYYYY       YYYYYYY
     GGG::::::::::::G                                                                          i::::i   f::::::::::::::::f Y:::::Y       Y:::::Y
   GG:::::::::::::::G                                                                           iiii   f::::::::::::::::::fY:::::Y       Y:::::Y
  G:::::GGGGGGGG::::G                                                                                  f::::::fffffff:::::fY::::::Y     Y::::::Y
 G:::::G       GGGGGG   ooooooooooo vvvvvvv           vvvvvvvaaaaaaaaaaaaa  nnnn  nnnnnnnn    iiiiiii  f:::::f       ffffffYYY:::::Y   Y:::::YYY
G:::::G               oo:::::::::::oov:::::v         v:::::v a::::::::::::a n:::nn::::::::nn  i:::::i  f:::::f                Y:::::Y Y:::::Y   
G:::::G              o:::::::::::::::ov:::::v       v:::::v  aaaaaaaaa:::::an::::::::::::::nn  i::::i f:::::::ffffff           Y:::::Y:::::Y    
G:::::G    GGGGGGGGGGo:::::ooooo:::::o v:::::v     v:::::v            a::::ann:::::::::::::::n i::::i f::::::::::::f            Y:::::::::Y     
G:::::G    G::::::::Go::::o     o::::o  v:::::v   v:::::v      aaaaaaa:::::a  n:::::nnnn:::::n i::::i f::::::::::::f             Y:::::::Y      
G:::::G    GGGGG::::Go::::o     o::::o   v:::::v v:::::v     aa::::::::::::a  n::::n    n::::n i::::i f:::::::ffffff              Y:::::Y       
G:::::G        G::::Go::::o     o::::o    v:::::v:::::v     a::::aaaa::::::a  n::::n    n::::n i::::i  f:::::f                    Y:::::Y       
 G:::::G       G::::Go::::o     o::::o     v:::::::::v     a::::a    a:::::a  n::::n    n::::n i::::i  f:::::f                    Y:::::Y       
  G:::::GGGGGGGG::::Go:::::ooooo:::::o      v:::::::v      a::::a    a:::::a  n::::n    n::::ni::::::if:::::::f                   Y:::::Y       
   GG:::::::::::::::Go:::::::::::::::o       v:::::v       a:::::aaaa::::::a  n::::n    n::::ni::::::if:::::::f                YYYY:::::YYYY    
     GGG::::::GGG:::G oo:::::::::::oo         v:::v         a::::::::::aa:::a n::::n    n::::ni::::::if:::::::f                Y:::::::::::Y    
        GGGGGG   GGGG   ooooooooooo            vvv           aaaaaaaaaa  aaaa nnnnnn    nnnnnniiiiiiiifffffffff                YYYYYYYYYYYYY    