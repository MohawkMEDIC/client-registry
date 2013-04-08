; Setup script for the MARC-HI Client Registry



[Setup]
AppId = {{65CA62B3-DC66-4597-8439-890B008CB5E5}
AppName = MEDIC Client Registry
AppVerName = 0.0.4
#ifdef BUNDLED
OutputBaseFilename = cr-setup-bundled-x64
#else
OutputBaseFilename = cr-setup-standalone-x64
#endif
LicenseFile = ..\License.rtf
AppPublisher = Mohawk College of Applied Arts and Technology
AppPublisherURL = http://te.marc-hi.ca
AppUpdatesURL = http://te.marc-hi.ca
DefaultDirName = {pf}\Mohawk College\Client Registry
AllowNoIcons = true
OutputDir = ..\dist
#ifdef DEBUG
Compression = none
#else
Compression = lzma2 
#endif
SolidCompression = false
AppCopyright = Copyright (C) 2011-2013 Mohawk College of Applied Arts and Technology
Uninstallable = true
ArchitecturesAllowed = x64
DefaultGroupName = Mohawk College\Client Registry
WizardSmallImageFile = .\install-small.bmp
WizardImageFile = .\install.bmp
[Languages]
Name: english; MessagesFile: compiler:Default.isl

[Files]
#ifdef BUNDLED
Source: .\installsupp\postgresql-9.2.4-1-windows-x64.exe; DestDir: {tmp}; Flags:dontcopy
#endif
Source: .\installsupp\dotNetFx40_Full_setup.exe; DestDir: {tmp} ; Flags: dontcopy

; SetupMARC.Everest.Connectors.WCF.dll
Source: ..\bin\release\Config\Everest\PDQ.etpl; DestDir: {app}\config\everest; Flags:ignoreversion; Components:msg\pixv3
Source: ..\bin\release\Config\Everest\PIX.etpl; DestDir: {app}\config\everest; Flags:ignoreversion; Components:msg\pixv3
Source: ..\bin\release\Config\Everest\PCS.etpl; DestDir: {app}\config\everest; Flags:ignoreversion; Components:msg\ca
Source: ..\bin\release\MARC.Everest.dll; DestDir:{app}; Flags:ignoreversion; Components:msg\ca msg\pixv3 notif
Source: ..\bin\release\MARC.Everest.Formatters.XML.Datatypes.R1.dll; DestDir:{app}; Flags:ignoreversion; Components:msg\ca msg\pixv3 notif
Source: ..\bin\release\MARC.Everest.Formatters.XML.ITS1.dll; DestDir:{app}; Flags:ignoreversion; Components:msg\ca msg\pixv3 notif
Source: ..\bin\release\MARC.Everest.Connectors.WCF.dll; DestDir:{app}; Flags:ignoreversion; Components:msg\ca msg\pixv3 notif
Source: ..\bin\release\MARC.Everest.RMIM.CA.R020402.dll; DestDir:{app}; Flags:ignoreversion; Components:msg\ca
Source: ..\bin\release\MARC.Everest.RMIM.UV.NE2008.dll; DestDir:{app}; Flags:ignoreversion; Components:msg\pixv3 notif
Source: ..\bin\release\MARC.HI.EHRS.CR.Core.dll; DestDir:{app}; Flags:ignoreversion; Components:core
Source: ..\bin\release\MARC.HI.EHRS.CR.Messaging.Admin.dll; DestDir:{app}; Flags:ignoreversion; Components:msg\admin
Source: ..\bin\release\MARC.HI.EHRS.CR.Messaging.Everest.dll; DestDir:{app}; Flags:ignoreversion; Components:msg\ca msg\pixv3
Source: ..\bin\release\MARC.HI.EHRS.CR.Messaging.HL7.dll; DestDir:{app}; Flags:ignoreversion; Components:msg\hl7
Source: ..\bin\release\MARC.HI.EHRS.CR.Messaging.PixPdqv2.dll; DestDir:{app}; Flags:ignoreversion; Components:msg\hl7
Source: ..\bin\release\MARC.HI.EHRS.CR.Notification.PixPdq.dll; DestDir:{app}; Flags:ignoreversion; Components:notif
Source: ..\bin\release\MARC.HI.EHRS.CR.Persistence.Data.dll; DestDir:{app}; Flags:ignoreversion; Components:core
Source: ..\bin\release\MARC.HI.EHRS.QM.Core.dll; DestDir:{app}; Flags:ignoreversion; Components:core
Source: ..\bin\release\MARC.HI.EHRS.QM.Persistence.Data.dll; DestDir:{app}; Flags:ignoreversion; Components:core
Source: ..\bin\release\MARC.HI.EHRS.SVC.Auditing.Atna.dll; DestDir:{app}; Flags:ignoreversion; Components:core
Source: ..\bin\release\MARC.HI.EHRS.SVC.Auditing.dll; DestDir:{app}; Flags:ignoreversion; Components:core
Source: ..\bin\release\MARC.HI.EHRS.SVC.ClientIdentity.dll; DestDir:{app}; Flags:ignoreversion; Components:core
Source: ..\bin\release\MARC.HI.EHRS.SVC.ConfigurationApplciation.dll; DestDir:{app}; Flags:ignoreversion; Components:core
Source: ..\bin\release\MARC.HI.EHRS.SVC.Configurator.PostgreSql9.dll; DestDir:{app}; Flags:ignoreversion; Components:core
Source: ..\bin\release\MARC.HI.EHRS.SVC.Core.ComponentModel.dll; DestDir:{app}; Flags:ignoreversion; Components:core
Source: ..\bin\release\MARC.HI.EHRS.SVC.Core.dll; DestDir:{app}; Flags:ignoreversion; Components:core
Source: ..\bin\release\MARC.HI.EHRS.SVC.Core.Timer.dll; DestDir:{app}; Flags:ignoreversion; Components:core
Source: ..\bin\release\MARC.HI.EHRS.SVC.DecisionSupport.dll; DestDir:{app}; Flags:ignoreversion; Components:core
Source: ..\bin\release\MARC.HI.EHRS.SVC.HealthWorkerIdentity.dll; DestDir:{app}; Flags:ignoreversion; Components:core
Source: ..\bin\release\MARC.HI.EHRS.SVC.Localization.dll; DestDir:{app}; Flags:ignoreversion; Components:core
Source: ..\bin\release\MARC.HI.EHRS.SVC.Messaging.Everest.dll; DestDir:{app}; Flags:ignoreversion; Components:msg\ca msg\pixv3
Source: ..\bin\release\MARC.HI.EHRS.SVC.Messaging.Multi.dll; DestDir:{app}; Flags:ignoreversion; Components:msg msg\ca msg\pixv3 msg\hl7
Source: ..\bin\release\MARC.HI.EHRS.SVC.Messaging.Persistence.Data.dll; DestDir:{app}; Flags:ignoreversion; Components:core
Source: ..\bin\release\MARC.HI.EHRS.SVC.PolicyEnforcement.dll; DestDir:{app}; Flags:ignoreversion; Components:core
Source: ..\bin\release\MARC.HI.EHRS.SVC.Subscription.Core.dll; DestDir:{app}; Flags:ignoreversion; Components:msg\rss
Source: ..\bin\release\MARC.HI.EHRS.SVC.Subscription.Data.dll; DestDir:{app}; Flags:ignoreversion; Components:msg\rss
Source: ..\bin\release\MARC.HI.EHRS.SVC.Terminology.dll; DestDir:{app}; Flags:ignoreversion; Components:core
Source: ..\bin\release\MohawkCollege.Util.Console.Parameters.dll; DestDir:{app}; Flags:ignoreversion; Components:core
Source: ..\bin\release\Mono.Security.dll; DestDir:{app}; Flags:ignoreversion; Components:core
Source: ..\bin\release\NHapi.Base.dll; DestDir:{app}; Flags:ignoreversion; Components:msg\hl7
Source: ..\bin\release\Config\HAPI\PDQ.htpl; DestDir: {app}\config\hapi; Flags:ignoreversion; Components:msg\hl7
Source: ..\bin\release\Config\HAPI\PIX.htpl; DestDir: {app}\config\hapi; Flags:ignoreversion; Components:msg\hl7
Source: ..\bin\release\Config\HAPI\NSP.htpl; DestDir: {app}\config\hapi; Flags:ignoreversion; Components:msg\hl7
Source: ..\bin\release\NHapi.Model.V231.dll; DestDir:{app}; Flags:ignoreversion; Components:msg\hl7
Source: ..\bin\release\NHapi.Model.V25.dll; DestDir:{app}; Flags:ignoreversion; Components:msg\hl7
Source: ..\bin\release\Npgsql.dll; DestDir:{app}; Flags:ignoreversion; Components:core
Source: ..\bin\release\policy.2.0.Npgsql.dll; DestDir:{app}; Flags:ignoreversion; Components:core
Source: ..\bin\release\ClientRegistry.exe; DestDir:{app}; Flags:ignoreversion; Components:core
Source: ..\bin\release\Configurator.exe; DestDir:{app}; Flags:ignoreversion; Components:core
Source: ..\bin\release\Configurator.exe.config; DestDir:{app}; Flags:ignoreversion; Components:core
Source: ..\bin\release\policy.2.0.Npgsql.config; DestDir:{app}; Flags:ignoreversion; Components:core
Source: ..\bin\release\DefaultOids.xml; DestDir: {app}; Flags:ignoreversion; Components:core
Source: ..\bin\release\ClientRegistry.en.xml; DestDir: {app}; Flags:ignoreversion; Components:core
Source: ..\bin\release\SQL\*.*; DestDir: {app}\sql; Flags:recursesubdirs ignoreversion; Components:core;
Source: ..\Solution Items\SQL\*.*; DestDir: {app}\sql; Flags:recursesubdirs ignoreversion; Components:core;
Source: ..\*; DestDir: {app}\src; Flags: ignoreversion recursesubdirs; Excludes: *.vssscc, *.dump, *.xap, ApiExplorer, Samples,*.vspscc, *.cache,*.resources,*.exe,*.exe.config,*.dll.config,*.pdb,MARC.*.xml,*.dll, *.iss, *.chm, *.xsd, *.wsdl, *.*mif, Solution Items, bin; Components: src
Source: ..\Solution Items\*.dll; DestDir: {app}\src\Solution Items; Flags: ignoreversion recursesubdirs; Components: src

[Types]
Name: full; Description: Complete Installation
Name: ca; Description: pan-Canadian Client Registry
Name: pix; Description: PIX Manager & PDQ Supplier (HL7v2.x)
Name: pixv3; Description: PIX Manager & PDQ Supplier (HL7v3)
Name: pixall; Description: PIX Manager & PDQ Supplier (All)
Name: custom; Description: Custom Installation; Flags: iscustom

[Components]
Name: core; Description: Client Registry Core; Types: full ca pix pixv3 pixall
Name: msg; Description: Messaging Support;  Types: full ca pix pixv3 pixall
Name: msg\hl7; Description: PIX/PDQ Interface;  Types: full pix pixall
Name: msg\pixv3; Description: PIXv3/PDQv3 Interface;  Types: full pixv3 pixall
Name: msg\ca; Description: HL7v3 pan-Canadian Interface;  Types: full ca 
Name: msg\admin; Description: Administrative Interface;  Types: full
Name: msg\rss; Description: Subscription Interface;  Types: full
Name: notif; Description: PIXv3 Notifications;  Types: full pix pixv3 pixall
Name: src; Description: Source Code; 

[Icons]
Name: {group}\Client Registry Configuration; FileName: {app}\Configurator.exe; Components:core
Name: {group}\MARC-HI Wiki; FileName: http://wiki.marc-hi.ca/
Name: {group}\{cm:UninstallProgram,Client Registry}; Filename: {uninstallexe}

[UninstallRun]
#ifndef DEBUG
Filename: "{dotnet40}\ngen.exe"; Parameters: "uninstall ""{app}\MARC.Everest.RMIM.UV.NE2008.dll"" /nologo /silent" ; Components:msg\pixv3 notif; StatusMsg: "Removing Native Assembly : MARC.Everest.RMIM.UV.NE2008"; Flags:runhidden
Filename: "{dotnet40}\ngen.exe"; Parameters: "uninstall ""{app}\MARC.Everest.RMIM.CA.R020402.dll"" /nologo /silent" ; Components:msg\ca; StatusMsg: "Removing Native Assembly : MARC.Everest.RMIM.CA.R020402"; Flags:runhidden
Filename: "{dotnet40}\ngen.exe"; Parameters: "uninstall ""{app}\MARC.Everest.dll"" /nologo /silent" ; Components:msg\ca msg\pixv3 notif; StatusMsg: "Removing Native Assembly : MARC.Everest"; Flags:runhidden
Filename: "{dotnet40}\ngen.exe"; Parameters: "uninstall ""{app}\MARC.Everest.Formatters.XML.ITS1.dll"" /nologo /silent" ; Components:msg\ca msg\pixv3 notif; StatusMsg: "Removing Native Assembly : MARC.Everest.Formatters.XML.ITS1"; Flags:runhidden
Filename: "{dotnet40}\ngen.exe"; Parameters: "uninstall ""{app}\MARC.Everest.Formatters.XML.DataTypes.R1.dll"" /nologo /silent" ; Components:msg\ca msg\pixv3 notif; StatusMsg: "Removing Native Assembly : MARC.Everest.Formatters.XML.Datatypes.R1"; Flags:runhidden
Filename: "{dotnet40}\ngen.exe"; Parameters: "uninstall ""{app}\MARC.Everest.Connectors.WCF.dll"" /nologo /silent" ; Components:msg\ca msg\pixv3 notif; StatusMsg: "Removing Native Assembly : MARC.Everest.Connectors.WCF"; Flags:runhidden
Filename: "{dotnet40}\ngen.exe"; Parameters: "uninstall ""{app}\MARC.HI.EHRS.CR.Core.dll"" /nologo /silent" ; Components:core; StatusMsg: "Removing Native Assembly : MARC.HI.EHRS.CR.Core"; Flags:runhidden
Filename: "{dotnet40}\ngen.exe"; Parameters: "uninstall ""{app}\MARC.HI.EHRS.CR.Messaging.Admin.dll"" /nologo /silent" ; Components:msg\admin; StatusMsg: "Removing Native Assembly : MARC.HI.EHRS.CR.Core.Messaging.Admin"; Flags:runhidden
Filename: "{dotnet40}\ngen.exe"; Parameters: "uninstall ""{app}\MARC.HI.EHRS.CR.Messaging.Everest.dll"" /nologo /silent" ; Components:msg\ca msg\pixv3; StatusMsg: "Removing Native Assembly : MARC.HI.EHRS.CR.Core.Messaging.Everest"; Flags:runhidden
Filename: "{dotnet40}\ngen.exe"; Parameters: "uninstall ""{app}\MARC.HI.EHRS.CR.Messaging.HL7.dll"" /nologo /silent" ; Components:msg\hl7; StatusMsg: "Removing Native Assembly : MARC.HI.EHRS.CR.Core.Messaging.HL7"; Flags:runhidden
Filename: "{dotnet40}\ngen.exe"; Parameters: "uninstall ""{app}\MARC.HI.EHRS.CR.Messaging.PixPdqv2.dll"" /nologo /silent" ; Components:msg\hl7; StatusMsg: "Removing Native Assembly : MARC.HI.EHRS.CR.Core.Messaging.PixPdqv2"; Flags:runhidden
Filename: "{dotnet40}\ngen.exe"; Parameters: "uninstall ""{app}\MARC.HI.EHRS.CR.Notification.PixPdq.dll"" /nologo /silent" ; Components:notif; StatusMsg: "Removing Native Assembly : MARC.HI.EHRS.CR.Core.Notification.PixPdq"; Flags:runhidden
Filename: "{dotnet40}\ngen.exe"; Parameters: "uninstall ""{app}\MARC.HI.EHRS.CR.Persistence.Data.dll"" /nologo /silent" ; Components:core; StatusMsg: "Removing Native Assembly : MARC.HI.EHRS.CR.Persistence.Data"; Flags:runhidden
Filename: "{dotnet40}\ngen.exe"; Parameters: "uninstall ""{app}\MARC.HI.EHRS.QM.Core.dll"" /nologo /silent" ; Components:core; StatusMsg: "Removing Native Assembly : MARC.HI.EHRS.QM.Core"; Flags:runhidden
Filename: "{dotnet40}\ngen.exe"; Parameters: "uninstall ""{app}\MARC.HI.EHRS.QM.Persistence.Data.dll"" /nologo /silent" ; Components:core; StatusMsg: "Removing Native Assembly : MARC.HI.QM.Persistence.Data"; Flags:runhidden
Filename: "{dotnet40}\ngen.exe"; Parameters: "uninstall ""{app}\MARC.HI.EHRS.SVC.Auditing.Atna.dll"" /nologo /silent" ; Components:core; StatusMsg: "Removing Native Assembly : MARC.HI.EHRS.SVC.Auditing.Atna"; Flags:runhidden
Filename: "{dotnet40}\ngen.exe"; Parameters: "uninstall ""{app}\MARC.HI.EHRS.SVC.Auditing.dll"" /nologo /silent" ; Components:core; StatusMsg: "Removing Native Assembly : MARC.HI.EHRS.SVC.Auditing"; Flags:runhidden
Filename: "{dotnet40}\ngen.exe"; Parameters: "uninstall ""{app}\MARC.HI.EHRS.SVC.ClientIdentity.dll"" /nologo /silent" ; Components:core; StatusMsg: "Removing Native Assembly : MARC.HI.EHRS.SVC.ClientIdentity"; Flags:runhidden
Filename: "{dotnet40}\ngen.exe"; Parameters: "uninstall ""{app}\MARC.HI.EHRS.SVC.ConfigurationApplciation.dll"" /nologo /silent" ; Components:core; StatusMsg: "Removing Native Assembly : MARC.HI.EHRS.SVC.ConfigurationApplication"; Flags:runhidden
Filename: "{dotnet40}\ngen.exe"; Parameters: "uninstall ""{app}\MARC.HI.EHRS.SVC.Configurator.PostgreSql9.dll"" /nologo /silent" ; Components:core; StatusMsg: "Removing Native Assembly : MARC.HI.EHRS.SVC.Configurator.Postgresql9"; Flags:runhidden
Filename: "{dotnet40}\ngen.exe"; Parameters: "uninstall ""{app}\MARC.HI.EHRS.SVC.Core.ComponentModel.dll"" /nologo /silent" ; Components:core; StatusMsg: "Removing Native Assembly : MARC.HI.EHRS.SVC.Core.ComponentMode"; Flags:runhidden
Filename: "{dotnet40}\ngen.exe"; Parameters: "uninstall ""{app}\MARC.HI.EHRS.SVC.Core.dll"" /nologo /silent" ; Components:core; StatusMsg: "Removing Native Assembly : MARC.HI.EHRS.SVC.Core"; Flags:runhidden
Filename: "{dotnet40}\ngen.exe"; Parameters: "uninstall ""{app}\MARC.HI.EHRS.SVC.Core.Timer.dll"" /nologo /silent" ; Components:core; StatusMsg: "Removing Native Assembly : MARC.HI.EHRS.SVC.Core.Timer"; Flags:runhidden
Filename: "{dotnet40}\ngen.exe"; Parameters: "uninstall ""{app}\MARC.HI.EHRS.SVC.DecisionSupport.dll"" /nologo /silent" ; Components:core; StatusMsg: "Removing Native Assembly : MARC.HI.EHRS.SVC.DecisionSupport"; Flags:runhidden
Filename: "{dotnet40}\ngen.exe"; Parameters: "uninstall ""{app}\MARC.HI.EHRS.SVC.HealthWorkerIdentity.dll"" /nologo /silent" ; Components:core; StatusMsg: "Removing Native Assembly : MARC.HI.EHRS.SVC.HealthWorkerIdentity"; Flags:runhidden
Filename: "{dotnet40}\ngen.exe"; Parameters: "uninstall ""{app}\MARC.HI.EHRS.SVC.Localization.dll"" /nologo /silent" ; Components:core; StatusMsg: "Removing Native Assembly : MARC.HI.EHRS.SVC.Localization"; Flags:runhidden
Filename: "{dotnet40}\ngen.exe"; Parameters: "uninstall ""{app}\MARC.HI.EHRS.SVC.Messaging.Everest.dll"" /nologo /silent" ; Components:msg\ca msg\pixv3; StatusMsg: "Removing Native Assembly : MARC.HI.EHRS.SVC.Messaging.Everest"; Flags:runhidden
Filename: "{dotnet40}\ngen.exe"; Parameters: "uninstall ""{app}\MARC.HI.EHRS.SVC.Messaging.Multi.dll"" /nologo /silent" ; Components:msg\hl7 msg\pixv3 msg\rss msg\admin msg\ca; StatusMsg: "Removing Native Assembly : MARC.HI.EHRS.SVC.Messaging.Multi"; Flags:runhidden
Filename: "{dotnet40}\ngen.exe"; Parameters: "uninstall ""{app}\MARC.HI.EHRS.SVC.Messaging.Persistence.Data.dll"" /nologo /silent" ; Components:core; StatusMsg: "Removing Native Assembly : MARC.HI.EHRS.SVC.Messaging.Persistence.Data"; Flags:runhidden
Filename: "{dotnet40}\ngen.exe"; Parameters: "uninstall ""{app}\MARC.HI.EHRS.SVC.PolicyEnforcement.dll"" /nologo /silent" ; Components:core; StatusMsg: "Removing Native Assembly : MARC.HI.EHRS.SVC.PolicyEnforcement"; Flags:runhidden
Filename: "{dotnet40}\ngen.exe"; Parameters: "uninstall ""{app}\MARC.HI.EHRS.SVC.Subscription.Core.dll"" /nologo /silent" ; Components:msg\rss; StatusMsg: "Removing Native Assembly : MARC.HI.EHRS.SVC.Subscription.Core"; Flags:runhidden
Filename: "{dotnet40}\ngen.exe"; Parameters: "uninstall ""{app}\MARC.HI.EHRS.SVC.Subscription.Data.dll"" /nologo /silent" ; Components:msg\rss; StatusMsg: "Removing Native Assembly : MARC.HI.EHRS.SVC.Subscription.Data"; Flags:runhidden
Filename: "{dotnet40}\ngen.exe"; Parameters: "uninstall ""{app}\MARC.HI.EHRS.SVC.Terminology.dll"" /nologo /silent" ; Components:core; StatusMsg: "Removing Native Assembly : MARC.HI.EHRS.SVC.Terminology"; Flags:runhidden
#endif

[Run]
#ifndef DEBUG
Filename: "{dotnet40}\ngen.exe"; Parameters: "install ""{app}\MARC.Everest.RMIM.UV.NE2008.dll"" /nologo /silent" ; Components:msg\pixv3 notif; StatusMsg: "Generating Native Assembly : MARC.Everest.RMIM.UV.NE2008"; Flags:runhidden
Filename: "{dotnet40}\ngen.exe"; Parameters: "install ""{app}\MARC.Everest.RMIM.CA.R020402.dll"" /nologo /silent" ; Components:msg\ca; StatusMsg: "Generating Native Assembly : MARC.Everest.RMIM.CA.R020402"; Flags:runhidden
Filename: "{dotnet40}\ngen.exe"; Parameters: "install ""{app}\MARC.Everest.dll"" /nologo /silent" ; Components:msg\ca msg\pixv3 notif; StatusMsg: "Generating Native Assembly : MARC.Everest"; Flags:runhidden
Filename: "{dotnet40}\ngen.exe"; Parameters: "install ""{app}\MARC.Everest.Formatters.XML.ITS1.dll"" /nologo /silent" ; Components:msg\ca msg\pixv3 notif; StatusMsg: "Generating Native Assembly : MARC.Everest.Formatters.XML.ITS1"; Flags:runhidden
Filename: "{dotnet40}\ngen.exe"; Parameters: "install ""{app}\MARC.Everest.Formatters.XML.DataTypes.R1.dll"" /nologo /silent" ; Components:msg\ca msg\pixv3 notif; StatusMsg: "Generating Native Assembly : MARC.Everest.Formatters.XML.Datatypes.R1"; Flags:runhidden
Filename: "{dotnet40}\ngen.exe"; Parameters: "install ""{app}\MARC.Everest.Connectors.WCF.dll"" /nologo /silent" ; Components:msg\ca msg\pixv3 notif; StatusMsg: "Generating Native Assembly : MARC.Everest.Connectors.WCF"; Flags:runhidden
Filename: "{dotnet40}\ngen.exe"; Parameters: "install ""{app}\MARC.HI.EHRS.CR.Core.dll"" /nologo /silent" ; Components:core; StatusMsg: "Generating Native Assembly : MARC.HI.EHRS.CR.Core"; Flags:runhidden
Filename: "{dotnet40}\ngen.exe"; Parameters: "install ""{app}\MARC.HI.EHRS.CR.Messaging.Admin.dll"" /nologo /silent" ; Components:msg\admin; StatusMsg: "Generating Native Assembly : MARC.HI.EHRS.CR.Core.Messaging.Admin"; Flags:runhidden
Filename: "{dotnet40}\ngen.exe"; Parameters: "install ""{app}\MARC.HI.EHRS.CR.Messaging.Everest.dll"" /nologo /silent" ; Components:msg\ca msg\pixv3; StatusMsg: "Generating Native Assembly : MARC.HI.EHRS.CR.Core.Messaging.Everest"; Flags:runhidden
Filename: "{dotnet40}\ngen.exe"; Parameters: "install ""{app}\MARC.HI.EHRS.CR.Messaging.HL7.dll"" /nologo /silent" ; Components:msg\hl7; StatusMsg: "Generating Native Assembly : MARC.HI.EHRS.CR.Core.Messaging.HL7"; Flags:runhidden
Filename: "{dotnet40}\ngen.exe"; Parameters: "install ""{app}\MARC.HI.EHRS.CR.Messaging.PixPdqv2.dll"" /nologo /silent" ; Components:msg\hl7; StatusMsg: "Generating Native Assembly : MARC.HI.EHRS.CR.Core.Messaging.PixPdqv2"; Flags:runhidden
Filename: "{dotnet40}\ngen.exe"; Parameters: "install ""{app}\MARC.HI.EHRS.CR.Notification.PixPdq.dll"" /nologo /silent" ; Components:notif; StatusMsg: "Generating Native Assembly : MARC.HI.EHRS.CR.Core.Notification.PixPdq"; Flags:runhidden
Filename: "{dotnet40}\ngen.exe"; Parameters: "install ""{app}\MARC.HI.EHRS.CR.Persistence.Data.dll"" /nologo /silent" ; Components:core; StatusMsg: "Generating Native Assembly : MARC.HI.EHRS.CR.Persistence.Data"; Flags:runhidden
Filename: "{dotnet40}\ngen.exe"; Parameters: "install ""{app}\MARC.HI.EHRS.QM.Core.dll"" /nologo /silent" ; Components:core; StatusMsg: "Generating Native Assembly : MARC.HI.EHRS.QM.Core"; Flags:runhidden
Filename: "{dotnet40}\ngen.exe"; Parameters: "install ""{app}\MARC.HI.EHRS.QM.Persistence.Data.dll"" /nologo /silent" ; Components:core; StatusMsg: "Generating Native Assembly : MARC.HI.QM.Persistence.Data"; Flags:runhidden
Filename: "{dotnet40}\ngen.exe"; Parameters: "install ""{app}\MARC.HI.EHRS.SVC.Auditing.Atna.dll"" /nologo /silent" ; Components:core; StatusMsg: "Generating Native Assembly : MARC.HI.EHRS.SVC.Auditing.Atna"; Flags:runhidden
Filename: "{dotnet40}\ngen.exe"; Parameters: "install ""{app}\MARC.HI.EHRS.SVC.Auditing.dll"" /nologo /silent" ; Components:core; StatusMsg: "Generating Native Assembly : MARC.HI.EHRS.SVC.Auditing"; Flags:runhidden
Filename: "{dotnet40}\ngen.exe"; Parameters: "install ""{app}\MARC.HI.EHRS.SVC.ClientIdentity.dll"" /nologo /silent" ; Components:core; StatusMsg: "Generating Native Assembly : MARC.HI.EHRS.SVC.ClientIdentity"; Flags:runhidden
Filename: "{dotnet40}\ngen.exe"; Parameters: "install ""{app}\MARC.HI.EHRS.SVC.ConfigurationApplciation.dll"" /nologo /silent" ; Components:core; StatusMsg: "Generating Native Assembly : MARC.HI.EHRS.SVC.ConfigurationApplication"; Flags:runhidden
Filename: "{dotnet40}\ngen.exe"; Parameters: "install ""{app}\MARC.HI.EHRS.SVC.Configurator.PostgreSql9.dll"" /nologo /silent" ; Components:core; StatusMsg: "Generating Native Assembly : MARC.HI.EHRS.SVC.Configurator.Postgresql9"; Flags:runhidden
Filename: "{dotnet40}\ngen.exe"; Parameters: "install ""{app}\MARC.HI.EHRS.SVC.Core.ComponentModel.dll"" /nologo /silent" ; Components:core; StatusMsg: "Generating Native Assembly : MARC.HI.EHRS.SVC.Core.ComponentMode"; Flags:runhidden
Filename: "{dotnet40}\ngen.exe"; Parameters: "install ""{app}\MARC.HI.EHRS.SVC.Core.dll"" /nologo /silent" ; Components:core; StatusMsg: "Generating Native Assembly : MARC.HI.EHRS.SVC.Core"; Flags:runhidden
Filename: "{dotnet40}\ngen.exe"; Parameters: "install ""{app}\MARC.HI.EHRS.SVC.Core.Timer.dll"" /nologo /silent" ; Components:core; StatusMsg: "Generating Native Assembly : MARC.HI.EHRS.SVC.Core.Timer"; Flags:runhidden
Filename: "{dotnet40}\ngen.exe"; Parameters: "install ""{app}\MARC.HI.EHRS.SVC.DecisionSupport.dll"" /nologo /silent" ; Components:core; StatusMsg: "Generating Native Assembly : MARC.HI.EHRS.SVC.DecisionSupport"; Flags:runhidden
Filename: "{dotnet40}\ngen.exe"; Parameters: "install ""{app}\MARC.HI.EHRS.SVC.HealthWorkerIdentity.dll"" /nologo /silent" ; Components:core; StatusMsg: "Generating Native Assembly : MARC.HI.EHRS.SVC.HealthWorkerIdentity"; Flags:runhidden
Filename: "{dotnet40}\ngen.exe"; Parameters: "install ""{app}\MARC.HI.EHRS.SVC.Localization.dll"" /nologo /silent" ; Components:core; StatusMsg: "Generating Native Assembly : MARC.HI.EHRS.SVC.Localization"; Flags:runhidden
Filename: "{dotnet40}\ngen.exe"; Parameters: "install ""{app}\MARC.HI.EHRS.SVC.Messaging.Everest.dll"" /nologo /silent" ; Components:msg\ca msg\pixv3; StatusMsg: "Generating Native Assembly : MARC.HI.EHRS.SVC.Messaging.Everest"; Flags:runhidden
Filename: "{dotnet40}\ngen.exe"; Parameters: "install ""{app}\MARC.HI.EHRS.SVC.Messaging.Multi.dll"" /nologo /silent" ; Components:msg\hl7 msg\pixv3 msg\rss msg\admin msg\ca; StatusMsg: "Generating Native Assembly : MARC.HI.EHRS.SVC.Messaging.Multi"; Flags:runhidden
Filename: "{dotnet40}\ngen.exe"; Parameters: "install ""{app}\MARC.HI.EHRS.SVC.Messaging.Persistence.Data.dll"" /nologo /silent" ; Components:core; StatusMsg: "Generating Native Assembly : MARC.HI.EHRS.SVC.Messaging.Persistence.Data"; Flags:runhidden
Filename: "{dotnet40}\ngen.exe"; Parameters: "install ""{app}\MARC.HI.EHRS.SVC.PolicyEnforcement.dll"" /nologo /silent" ; Components:core; StatusMsg: "Generating Native Assembly : MARC.HI.EHRS.SVC.PolicyEnforcement"; Flags:runhidden
Filename: "{dotnet40}\ngen.exe"; Parameters: "install ""{app}\MARC.HI.EHRS.SVC.Subscription.Core.dll"" /nologo /silent" ; Components:msg\rss; StatusMsg: "Generating Native Assembly : MARC.HI.EHRS.SVC.Subscription.Core"; Flags:runhidden
Filename: "{dotnet40}\ngen.exe"; Parameters: "install ""{app}\MARC.HI.EHRS.SVC.Subscription.Data.dll"" /nologo /silent" ; Components:msg\rss; StatusMsg: "Generating Native Assembly : MARC.HI.EHRS.SVC.Subscription.Data"; Flags:runhidden
Filename: "{dotnet40}\ngen.exe"; Parameters: "install ""{app}\MARC.HI.EHRS.SVC.Terminology.dll"" /nologo /silent" ; Components:core; StatusMsg: "Generating Native Assembly : MARC.HI.EHRS.SVC.Terminology"; Flags:runhidden
#endif
Filename: "{app}\configurator.exe"; Description: "Configure the Client Registry"; Flags: postinstall nowait skipifsilent

; Components
[Code]
var
  dotNetNeeded: boolean;
  memoDependenciesNeeded: string;
  psqlPageId : integer;
  chkInstallPSQL : TCheckBox;
  txtPostgresSU, txtPostgresSUPass : TEdit;

const
  dotnetRedistURL = '{tmp}\dotNetFx40_Full_setup.exe';
  // local system for testing...
  // dotnetRedistURL = 'http://192.168.1.1/dotnetfx.exe';


function InitializeSetup(): Boolean;

begin
 
  Result := true;
  dotNetNeeded := false;

  
  if(not DirExists(ExpandConstant('{win}\Microsoft.NET\Framework\v4.0.30319'))) then begin
    dotNetNeeded := true;
    if (not IsAdminLoggedOn()) then begin
      MsgBox('GPMR needs the Microsoft .NET Framework 4 to be installed by an Administrator', mbInformation, MB_OK);
      Result := false;
    end else begin
      memoDependenciesNeeded := memoDependenciesNeeded + '      .NET Framework 4' #13;
    end;
  end;

end;

function PrepareToInstall(var needsRestart:Boolean): String;
var
  hWnd: Integer;
  ResultCode : integer;
  uninstallString : string;
begin
    
    EnableFsRedirection(true);

    #ifdef BUNDLED
    if (chkInstallPSQL.Checked) then begin
      ExtractTemporaryFile('postgresql-9.2.4-1-windows-x64.exe');
      if Exec(ExpandConstant('{tmp}\postgresql-9.2.4-1-windows-x64.exe'), '--mode unattended --superaccount ' + txtPostgresSU.Text + ' --superpassword ' + txtPostgresSUPass.Text + ' --servicename PostgreSQLCR --install_runtimes 1 --prefix "' + ExpandConstant('{app}\postgresql') + '" --datadir "' + ExpandConstant('{app}\postgresql\data') + '"', '', SW_SHOW, ewWaitUntilTerminated, ResultCode) then begin
          // handle success if necessary; ResultCode contains the exit code
          if not (ResultCode = 0) then begin
            Result := 'PostgreSQL Install Failed';
          end;
        end else begin
          // handle failure if necessary; ResultCode contains the error code
            Result := 'PostgreSQL Install Failed';
        end;
      end;
    #endif
    if (Result = '') and (dotNetNeeded = true) then begin
      ExtractTemporaryFile('dotNetFx40_Full_setup.exe');
      if Exec(ExpandConstant(dotnetRedistURL), '/passive /norestart', '', SW_SHOW, ewWaitUntilTerminated, ResultCode) then begin
          // handle success if necessary; ResultCode contains the exit code
          if not (ResultCode = 0) then begin
            Result := '.NET Framework 4 is Required';
          end;
        end else begin
          // handle failure if necessary; ResultCode contains the error code
            Result := '.NET Framework 4 is Required';
        end;
    end;

end;

function UpdateReadyMemo(Space, NewLine, MemoUserInfoInfo, MemoDirInfo, MemoTypeInfo, MemoComponentsInfo, MemoGroupInfo, MemoTasksInfo: String): String;
var
  s: string;

begin
  if memoDependenciesNeeded <> '' then s := s + 'Dependencies that will be automatically downloaded And installed:' + NewLine + memoDependenciesNeeded + NewLine;

  s := s + MemoDirInfo + NewLine;

  Result := s
end;


function PSQL_CreatePage(PreviousPageId : Integer) : Integer  ;
var
  Page : TWizardPage;
  lblSU, lblSUPwd, lblDescription : TLabel;
  
begin
  Page := CreateCustomPage( PreviousPageId, ExpandConstant('Install PostgreSQL'), ExpandConstant('Setup can install PostgreSQL 9.2'));
  
  // Select mode
  lblDescription := TLabel.Create(Page);
	with lblDescription do begin
		Parent := Page.Surface;
		Caption := ExpandConstant('Setup can install Enterprise DB''s Windows version of PostgreSQL 9.2.4 on this computer. You do not need to do this if you have another computer running PostgreSQL 9.1 or higher.');
    WordWrap := true;
		Left := ScaleX(5);
		Top := ScaleY(8);
		Width := ScaleX(410);
		Height := ScaleY(100);
	end;
	
	// Check to install PSQL
	chkInstallPSQL := TCheckBox.Create(Page);
	with chkInstallPSQL do begin
		Parent := Page.Surface;
		Caption := ExpandConstant('Install PostgreSQL 9.2.4');
		Left := ScaleX(5);
		Top := ScaleY(60);
		Width := ScaleX(348);
		Height := ScaleY(32);
		Checked := true;
	end;
	
  // Username
  lblSU := TLabel.Create(Page);
  with lblSU do begin
    Parent := Page.Surface;
    Caption := 'Superuser Account:';
		Left := ScaleX(5);
		Top := ScaleY(90);
		Width := ScaleX(128);
		Height := ScaleY(32);
  end; 
  txtPostgresSU := TEdit.Create(Page);
  with txtPostgresSU do begin
    Parent := Page.Surface;
		Left := ScaleX(128);
		Top := ScaleY(90);
		Width := ScaleX(128);
		Height := ScaleY(32);
  end;

  // Pass
  lblSUPwd := TLabel.Create(Page);
  with lblSUPwd do begin
    Parent := Page.Surface;
    Caption := 'Password:';
		Left := ScaleX(5);
		Top := ScaleY(112);
		Width := ScaleX(128);
		Height := ScaleY(32);
  end; 
  txtPostgresSUPass := TEdit.Create(Page);
  with txtPostgresSUPass do begin
    Parent := Page.Surface;
		Left := ScaleX(128);
		Top := ScaleY(112);
		Width := ScaleX(128);
    PasswordChar := '*';
		Height := ScaleY(32);
  end;

  // Check 
	Result := Page.ID;
end;

function NextButtonClick(CurPageID: Integer): Boolean;
begin
  if(CurPageID = psqlPageId) then begin
    if(chkInstallPSQL.Checked = true and ((txtPostgresSU.Text = '') or (txtPostgresSUPass.Text = ''))) then begin
      MsgBox('When installing PostgreSQL you must supply a superuser name and password', mbInformation, MB_OK);
      Result := false;
    end else
      Result := true;
  end else
    Result := true;
end;

procedure InitializeWizard();
begin
#ifdef BUNDLED
  	psqlPageId := PSQL_CreatePage(wpWelcome);
#endif
end;
