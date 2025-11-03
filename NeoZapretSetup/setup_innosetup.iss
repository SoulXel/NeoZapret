[Setup]
AppName=NeoZapret
AppVersion=2.1.0
AppPublisher=Lu1ky
AppPublisherURL=https://github.com/Lu1ky
AppSupportURL=https://github.com/Lu1ky
DefaultDirName={commonpf}\NeoZapret
DefaultGroupName=NeoZapret
OutputBaseFilename=NeoZapret-Setup
OutputDir=.
Compression=lzma
SolidCompression=yes
PrivilegesRequired=admin
LicenseFile=
Uninstallable=yes
WizardImageFile=
WizardSmallImageFile=
WizardImageStretch=no
SetupIconFile=
DisableWelcomePage=no
DisableReadyPage=no
DisableFinishedPage=no
CreateUninstallIcon=yes
MinVersion=0,6.1

[Languages]
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"

[Messages]
SetupWindowTitle=Установка NeoZapret
WelcomeLabel1=Добро пожаловать в программу установки NeoZapret
WelcomeLabel2=Эта программа установит NeoZapret на ваш компьютер.%n%nОбход блокировок РФ 2025
ClickNext=Нажмите Далее для продолжения

[Tasks]
Name: "desktopicon"; Description: "Создать ярлык на рабочем столе"; GroupDescription: "Дополнительные ярлыки:"
Name: "quicklaunchicon"; Description: "Создать ярлык в панели быстрого запуска"; GroupDescription: "Дополнительные ярлыки:"; Flags: unchecked

[Files]
Source: "..\NeoZapret\bin\Release\net472\NeoZapret.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\bin\*"; DestDir: "{app}\bin"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\lists\*"; DestDir: "{app}\lists"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\README.md"; DestDir: "{app}"; DestName: "README.txt"; Flags: ignoreversion

[Icons]
Name: "{group}\NeoZapret"; Filename: "{app}\NeoZapret.exe"; Description: "Обход блокировок РФ 2025"
Name: "{group}\Uninstall NeoZapret"; Filename: "{uninstallexe}"
Name: "{autodesktop}\NeoZapret"; Filename: "{app}\NeoZapret.exe"; Tasks: desktopicon
Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\NeoZapret"; Filename: "{app}\NeoZapret.exe"; Tasks: quicklaunchicon

[Run]
Filename: "{app}\NeoZapret.exe"; Description: "Запустить NeoZapret"; Flags: nowait postinstall skipifsilent

[Code]
procedure InitializeWizard;
begin
  WizardForm.LicenseAcceptedRadio.Checked := True;
end;

