#include <Windows.h>
#include <stdio.h>
#include <assert.h>
#include "steam_api.h"

const AppId_t GAME_APP_ID			= 516750;
const char *const GAME_APP_ID_STR	= "516750";
const char *const ExecutableName	= "mysummercar.exe";

#define GAME_FULL_NAME				"My Summer Car"
#define PROJECT_FULL_NAME			GAME_FULL_NAME " Multiplayer"

/**
 * Inject DLL into process.
 *
 * @param[in] process Process handle.
 * @param[in] dllPath DLL path.
 * @return @c false on case of injection failure @c true otherwise.
 */
bool InjectDll(const HANDLE process, const char *const dllPath)
{
	const size_t libPathLen = strlen(dllPath) + 1;
	SIZE_T bytesWritten = 0;

	void *const remoteLibPath = VirtualAllocEx(process, NULL, libPathLen, MEM_COMMIT, PAGE_READWRITE);
	if (!remoteLibPath) {
		return false;
	}

	if (!WriteProcessMemory(process, remoteLibPath, dllPath, libPathLen, &bytesWritten)) {
		VirtualFreeEx(process, remoteLibPath, 0, MEM_RELEASE);
		return false;
	}

	const HMODULE kernel32dll = GetModuleHandle("Kernel32");
	if (!kernel32dll) {
		VirtualFreeEx(process, remoteLibPath, 0, MEM_RELEASE);
		return false;
	}

	const FARPROC pfnLoadLibraryA = GetProcAddress(kernel32dll, "LoadLibraryA");
	if (!pfnLoadLibraryA) {
		FreeModule(kernel32dll);
		VirtualFreeEx(process, remoteLibPath, 0, MEM_RELEASE);
		return false;
	}

	const HANDLE hThread = CreateRemoteThread(process, NULL, 0, (LPTHREAD_START_ROUTINE)pfnLoadLibraryA, remoteLibPath, 0, NULL);
	if (!hThread) {
		FreeModule(kernel32dll);
		VirtualFreeEx(process, remoteLibPath, 0, MEM_RELEASE);
		return false;
	}

	WaitForSingleObject(hThread, INFINITE);
	CloseHandle(hThread);

	FreeModule(kernel32dll);
	VirtualFreeEx(process, remoteLibPath, 0, MEM_RELEASE);
	return true;
}

//! Steam api wrapper.
struct SteamWrapper
{
	bool Init(void)
	{
		if (!SteamAPI_IsSteamRunning()) {
			MessageBox(NULL, "To run " PROJECT_FULL_NAME " your Steam client must be running.", "Fatal error", MB_ICONERROR);
			return false;
		}

		if (!SteamAPI_Init()) {
			MessageBox(NULL, "Failed to initialize steam.", "Fatal error", MB_ICONERROR);
			return false;
		}

		// XXX: We may want to eventually handle steam errors here, some users have reported
		// that they were unable to play game as steam fails to initialize for them, in most cases
		// making OS privilages for both steam and game process to be the same level was solving the problem.
		return true;
	}

	~SteamWrapper(void)
	{
		SteamAPI_Shutdown();
	}
};

/**
 * Launcher entry point.
 *
 * @see https://msdn.microsoft.com/en-us/library/windows/desktop/ms633559(v=vs.85).aspx
 */
int WINAPI WinMain(HINSTANCE hInstance, HINSTANCE hPrevInstance, LPSTR lpCmdLine, int nCmdShow)
{
	SteamWrapper steam;
	if (!steam.Init()) {
		return 0;
	}

	ISteamApps *const steamApps = SteamApps();

	if (!steamApps->BIsAppInstalled(GAME_APP_ID)) {
		MessageBox(NULL, "To run " PROJECT_FULL_NAME " you need to have installed " GAME_FULL_NAME " game.", "Fatal error", MB_ICONERROR);
		return 0;
	}

	char installFolder[MAX_PATH] = { 0 };
	steamApps->GetAppInstallDir(GAME_APP_ID, installFolder, MAX_PATH);

	char gameExePath[MAX_PATH] = { 0 };
	sprintf(gameExePath, "%s\\%s", installFolder, ExecutableName);

	STARTUPINFO startupInfo = { 0 };
	PROCESS_INFORMATION processInformation = { 0 };
	startupInfo.cb = sizeof(startupInfo);

	SetEnvironmentVariable("SteamAppID", GAME_APP_ID_STR);

	if (GetFileAttributes(gameExePath) == INVALID_FILE_ATTRIBUTES) {
		MessageBox(NULL, "Unable to find game .exe file.", "Fatal error", MB_ICONERROR);
		return 0;
	}

	if (!CreateProcess(gameExePath, NULL, NULL, NULL, FALSE, CREATE_SUSPENDED, NULL, installFolder, &startupInfo, &processInformation)) {
		MessageBox(NULL, "Cannot create game process.", "Fatal error", MB_ICONERROR);
		return 0;
	}

	// Helper lambda used to show fatal error message and terminate the process.

	auto ShowFatalError = [&processInformation](const char *const message)
	{
		MessageBox(NULL, message, "Fatal error", MB_ICONERROR);
		TerminateProcess(processInformation.hProcess, 0);
	};

	// Ensure that player uses 64bit build of the game, back in the day
	// we were not supporting 64 bit version and asked users to switch to default_32bit
	// branch which may cause some of them still using them. Let them know that they have
	// to use default branch which is 64 bit as of 21.10.2018 and hopefully will be
	// like that forever and ever (cheers Epica fans :)).

	BOOL Wow64Process = FALSE;
	if (!IsWow64Process(processInformation.hProcess, &Wow64Process)) {
		ShowFatalError("Failed to determinate game architecture.");
		return 0;
	}

	// We don't need to check if os is 32bit as the launcher is 64bit only application.

	if (Wow64Process) {
		ShowFatalError("Mod only supports 64bit version of " GAME_FULL_NAME ". Please switch to default branch to play the mod.");
		return 0;
	}

	char cPath[MAX_PATH] = { 0 };
	GetModuleFileName(NULL, cPath, MAX_PATH);
	char injectorDllPath[MAX_PATH] = { 0 };

	size_t LauncherPathLength = strlen(cPath);
	for (size_t i = LauncherPathLength - 1; i > 0; --i) {
		if (cPath[i] == '\\') {
			cPath[i] = 0;
			break;
		}
	}

	sprintf(injectorDllPath, "%s\\MSCMPInjector.dll", cPath);

	if (GetFileAttributes(injectorDllPath) == INVALID_FILE_ATTRIBUTES) {
		ShowFatalError("Cannot find MSCMPInjector.dll file.");
		return 0;
	}

	if (!InjectDll(processInformation.hProcess, injectorDllPath)) {
		ShowFatalError("Could not inject dll into the game process. Please try launching the game again.");
		return 0;
	}

	ResumeThread(processInformation.hThread);
	return 1;
}

/* EOF */