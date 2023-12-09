
; --------------------------------
; Setup

  !include "MUI2.nsh"

  CRCCheck On

; ---------------------------------
; General

  Name "Knossos.NET"

  !ifndef PUBLISH_DIR
    !error "ERROR: PUBLISH_DIR is not defined!"
  !endif

  !ifndef OUTPUT_DIR
    !error "ERROR: OUTPUT_DIR is not defined!"
  !endif

  !ifndef ARCH
    !error "ERROR: ARCH is not defined!"
  !endif

  !ifdef VERSION
    OutFile "${OUTPUT_DIR}/Knossos.NET-${VERSION}-${ARCH}.exe"
  !else
    OutFile "${OUTPUT_DIR}/Knossos.NET-${ARCH}.exe"
  !endif

  ; Default install location
  InstallDir "$LOCALAPPDATA\Knossos.NET"

  ; Get install location from registry if available
  InstallDirRegKey HKCU "Software\KnossosNET\Knossos.NET" "InstallPath"

  ; Request privileges for Vista+
  RequestExecutionLevel user

; ---------------------------------
; UI Config

  !define MUI_COMPONENTSPAGE_NODESC
  !define MUI_FINISHPAGE_RUN "$INSTDIR\Knossos.NET.exe"
  !define MUI_FINISHPAGE_RUN_TEXT "Run Knossos.NET"
  !define MUI_FINISHPAGE_NOREBOOTSUPPORT

  !insertmacro MUI_PAGE_WELCOME
  !insertmacro MUI_PAGE_COMPONENTS
  !insertmacro MUI_PAGE_DIRECTORY
  !insertmacro MUI_PAGE_INSTFILES
  !insertmacro MUI_PAGE_FINISH

  !insertmacro MUI_UNPAGE_WELCOME
  !insertmacro MUI_UNPAGE_CONFIRM
  !insertmacro MUI_UNPAGE_INSTFILES
  !insertmacro MUI_UNPAGE_FINISH

  !insertmacro MUI_LANGUAGE "English"

; ---------------------------------
; Sections

Section "Knossos.NET"
  SectionIn RO
  SetOutPath "$INSTDIR"

  File "${PUBLISH_DIR}/win-${ARCH}/Knossos.NET.exe"

  ; save install location
  WriteRegStr HKCU "Software\KnossosNET\Knossos.NET" "InstallPath" "$INSTDIR"

  ; Start menu entries
  CreateShortCut "$SMPROGRAMS\Knossos.NET.lnk" "$INSTDIR\Knossos.NET.exe" "" "$INSTDIR\Knossos.NET.exe" 0

  ; Add uninstall info to registry
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\Knossos.NET" "DisplayName" "Knossos.NET"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\Knossos.NET" "DisplayIcon" "$INSTDIR\Knossos.NET.exe"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\Knossos.NET" "UninstallString" "$INSTDIR\Uninstall.exe"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\Knossos.NET" "Publisher" "KnossosNET"

  !ifdef VERSION
    WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\Knossos.NET" "DisplayVersion" "${VERSION}"
  !endif

  WriteRegDWORD HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\Knossos.NET" "EstimatedSize" "98540"
  WriteRegDWORD HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\Knossos.NET" "NoModify" 1
  WriteRegDWORD HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\Knossos.NET" "NoRepair" 1

  WriteUninstaller "$INSTDIR\Uninstall.exe"
SectionEnd

Section "Uninstall"
  ; remove installed files/folders
  RMDir /r "$INSTDIR\*.*"
  RMDir "$INSTDIR"

  ; remove start menu entries
  Delete "$SMPROGRAMS\Knossos.NET.lnk"

  ; remove install info
  DeleteRegKey HKCU "Software\KnossosNET\Knossos.NET"

  ; remove uninstall info
  DeleteRegKey HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\Knossos.NET"
SectionEnd