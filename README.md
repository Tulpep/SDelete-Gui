SDelete Gui [![Build Status](https://ci.appveyor.com/api/projects/status/github/Tulpep/SDelete-Gui)](https://ci.appveyor.com/project/tulpep/SDelete-Gui)
===========

### Securely delete files with an easy-to-use right-click option.
![RightClick](Screenshots/RightClick.png)

### [Download now][1]

## Introduction
**SDelete Gui** is simple application that allows you to add, configure, and remove a "Secure Delete" option in the Windows context menu (right-click) of a selected file or folder. Secure Delete will **securely** and **permanently** delete all selected files and folders.  

**Note: There is NO confirmation dialog prior to deletion.  Once Secure Delete is clicked, all selected files and folders will be deleted and completely unrecoverable!**

This tool adds the right menu option but does not contain the logic for deletion. It uses [SDelete](http://technet.microsoft.com/en-us/sysinternals/bb897443.aspx) of [Microsoft Sysinternals](http://technet.microsoft.com/en-us/sysinternals/bb545021.aspx) written by [Mark Russinovich](http://blogs.technet.com/b/markrussinovich/) to perform the file and folder deletion using a United States Department of Defense compliant algorithm. Click on [this link][5] to see how the SDelete utility works. 

The application will download SDelete from Microsoft's web site in order to work properly. If the download fails, it will check if you already have SDelete in your System32 or SysWow64 folder.  In the event that SDelete could not be downloaded and it cannot be found on the hard drive, the application will display an error message and will not function correctly. 

## How to use the application
1) Download [SDelete-Gui.exe][1]
2) Excute the downloaded file
3) Click "Yes" on the UAC Prompt that shows up  
![UAC Dialog][UAC]
4) The configuration window opens up  
![Main Utility interface image][Utility]
5) Press the '+' or '-' buttons to increase or decrease the number of passes the deletion will use. The ENABLE button must be clicked to save the value  
  ![Change the number of passes image][ChangedPasses]
6) Press the ENABLE button to add the Secure Delete option to the context menu when you right-click on files and folders  
  ![Enable button image][EnableClicked]
7) Press the DISABLE button to remove the Secure Delete option from the same context menus  
  ![Disable button image][DisableClicked]

[1]: https://github.com/Tulpep/SDelete-Gui/releases/latest
[2]: http://technet.microsoft.com/en-us/sysinternals/bb897443.aspx
[3]: http://technet.microsoft.com/en-us/sysinternals/bb545021.aspx
[4]: http://blogs.technet.com/b/markrussinovich/
[5]: https://docs.microsoft.com/en-us/sysinternals/downloads/sdelete#how-sdelete-works
[UAC]: Screenshots/UACPrompt.png "UAC Prompt"
[Utility]: Screenshots/Gui.png "Main utility interface"
[ChangedPasses]: Screenshots/ChangedPasses.png "Increase or decrease the number of passes the deletion takes"
[EnableClicked]: Screenshots/EnableClicked.png "Enable button"
[DisableClicked]: Screenshots/DisableClicked.png "Disable button"
