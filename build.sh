#Clearing bins, objects and previous releases
rm -rf obj
rm -rf bin
rm -rf dist
#Updating build version in Program.cs
cat Program.cs | grep 'private static string build=' | sed 's/^.*="//' | sed 's/";//g' | while read -r line ; do
  oldline='private static string build="'$line'";'
  newline='private static string build="'$((line+1))'";'
  sed -i "s/$oldline/$newline/g" Program.cs
done

#Getting final versions
lastbuild=$(cat Program.cs | grep 'private static string build=' | sed 's/^.*="//' | sed 's/";//g' | sed '2!d')
actualversion=$(cat Program.cs | grep 'public static string ActualVersion=' | sed 's/^.*="//' | sed 's/";//g')

#Updating build version in LIST.js
cat WEB/list.json | grep '"build"' | sed 's/^.*:"//' | sed 's/",//g' | while read -r line ; do
  oldline='"build":"'$line'",'
  newline='"build":"'$lastbuild'",'
  sed -i "s/$oldline/$newline/g" WEB/list.json
done

#Making dev versions
xbuild KH2FM_Toolkit.sln /t:Build /p:Configuration=Debug
#Copying Hashlist so that we can build minimal user versions
cp Ressources/Hashlist.bin Ressources/Hashlist.bin23
cp Ressources/Hashlist.bin2 Ressources/Hashlist.bin
#Making user versions
xbuild KH2FM_Toolkit.sln /t:Build /p:Configuration=Release
#Copying back Hashlists
cp Ressources/Hashlist.bin Ressources/Hashlist.bin2
cp Ressources/Hashlist.bin23 Ressources/Hashlist.bin

#Copying all in dist directory
mkdir dist
cp obj/Debug/KH2FM_Toolkit.exe dist/toolkit_dev_other.exe
cp obj/Release/KH2FM_Toolkit.exe dist/toolkit_user_other.exe

echo "Done and ready to be uploaded! build version: $lastbuild version: $actualversion"
echo "Warning: Ensure that Readme and actual version has been updated, I can't update those automatically ;)"
