KH2FM_Toolkit
=============

This is a software used for modding the game Kingdom Hearts 2 (Final Mix).   
It is heavily unmaintained so beware! 

```This tool is able to extract and modify the iso of the game Kingdom Hearts 2(Final Mix)
It uses a list that gives all of the ISO resource names in clear (msg/jp/al.bar) instead of their hashes(0x56203d96).
The list isn't totally complete but all the files you should need got a name.

This tool can launch more than one patch and mix them instead to have to apply them one by one.
Simply drag 'n drop all patches you need to apply to the toolkit or, with the
windows cmd, execute the command(in the directory of KH2FM_Toolkit): KH2FM_Toolkit patch1.kh2patch patch2.kh2patch

You can change the name of the iso to modify simply by drag 'n drop him to the software or to use the command: "KH2FM_Toolkit [youroptions] YOURISO.ISO

Options:

[-help]: Extract this Readme
[-license]: Extract the license you agree by using this soft
[-extractor]: Launch the extractor. Instead of patching the game, the toolkit will extract him
[-exit]: Just stop the soft. Nothing else(Making the "return;" action")
[-batch]: Skipping all the "Console.Readline();"(when you need to press enter)and closing automatically the soft at the end.
[-patchmaker]: Launching the patchmaker.
[-advancedinfo]: To use after -extractor. This option will show advanced info about files extracted.
[-verifyiso]: Launch the SHA1 verifier. It will calculate the SHA1 hash of your iso for verify you have a good dump.
[-log]: Will redirect the text to a file /!\ Cannot mirror the text to the console & a file for now, you'll have a black screen but the soft will work /!\


Patchmaker Options(to put after the option -patchmaker):

[-xeeynamo]: Will create a patch with xeeynamo's encryption.
[-batch]: Skip all the "Console.Readline();"(when you need to press enter)and closing automatically the soft at the end. (Yes, another time)
[-version x]: Set the version to x. Need to be a entire number.
[-author x]: Set the author to x
[-changelog x]: Set the changelog to x
[-credits x]: Set the credits to x
[-skipchangelog]: Nothing is used for the changelogs, Changelog option is not shown at patching process
[-skipcredits]: Nothing is used for the credits, Changelog option is not shown at patching process
[-output something.kh2patch]: Set the output file to something.kh2patch
[-uselog]: It will load the file setted after the option and use it as a log file for automatically building patches with the patchmaker

Options asked:
[Relink to this filename:]: This will copy the content of the file chosen to your file.
[Compress this file?]: This will compress or no the file using internal compression of KH2(FM).
[Parent compressed file]: Just choose where to modify the file: in KH2, OVL, or the ISO.
[Should this file be added if he's not in the game?]: If the file don't exist, will try to create a new entry for this file.

When you want to write the patch file, just leave blank a filename, it will create it

```
