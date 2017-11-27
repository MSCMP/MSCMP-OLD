#include <Windows.h>
#include <stdio.h>

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
		VirtualFreeEx(process, remoteLibPath, sizeof(remoteLibPath), MEM_RELEASE);
		return false;
	}

	const HMODULE kernel32dll = GetModuleHandle("Kernel32");
	if (!kernel32dll) {
		VirtualFreeEx(process, remoteLibPath, sizeof(remoteLibPath), MEM_RELEASE);
		return false;
	}

	const FARPROC pfnLoadLibraryA = GetProcAddress(kernel32dll, "LoadLibraryA");
	if (!pfnLoadLibraryA) {
		FreeModule(kernel32dll);
		VirtualFreeEx(process, remoteLibPath, sizeof(remoteLibPath), MEM_RELEASE);
		return false;
	}

	const HANDLE hThread = CreateRemoteThread(process, NULL, 0, (LPTHREAD_START_ROUTINE)pfnLoadLibraryA, remoteLibPath, 0, NULL);
	if (!hThread) {
		FreeModule(kernel32dll);
		VirtualFreeEx(process, remoteLibPath, sizeof(remoteLibPath), MEM_RELEASE);
		return false;
	}

	WaitForSingleObject(hThread, INFINITE);
	CloseHandle(hThread);

	FreeModule(kernel32dll);
	VirtualFreeEx(process, remoteLibPath, sizeof(remoteLibPath), MEM_RELEASE);
	return true;
}

/**
 * Launcher entry point.
 *
 * @see https://msdn.microsoft.com/en-us/library/windows/desktop/ms633559(v=vs.85).aspx
 */
int WINAPI WinMain(HINSTANCE hInstance, HINSTANCE hPrevInstance, LPSTR lpCmdLine, int nCmdShow)
{

	const char installFolder[] = "J:\\Games\\steamapps\\common\\My Summer Car";
	//const char installFolder[] = "C:\\Program Files (x86)\\Steam\\steamapps\\common\\My Summer Car";
	const char ExecutableName[] = "mysummercar.exe";

	char gameExePath[MAX_PATH] = { 0 };
	sprintf(gameExePath, "%s\\%s", installFolder, ExecutableName);

	STARTUPINFO startupInfo = { 0 };
	PROCESS_INFORMATION processInformation = { 0 };
	startupInfo.cb = sizeof(startupInfo);

	SetEnvironmentVariable("SteamAppID", "516750");

	if (GetFileAttributes(gameExePath) == INVALID_FILE_ATTRIBUTES) {
		MessageBox(NULL, "Unable to find game .exe file.", "Fatal error", MB_ICONERROR);
		return 0;
	}

	if (!CreateProcess(gameExePath, NULL, NULL, NULL, FALSE, CREATE_SUSPENDED, NULL, installFolder, &startupInfo, &processInformation)) {
		MessageBox(NULL, "Cannot create game process.", "Fatal error", MB_ICONERROR);
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
		MessageBox(NULL, "Cannot find MSCMPInjector.dll file.", "Error", MB_ICONERROR);
		return 0;
	}

	if (!InjectDll(processInformation.hProcess, injectorDllPath)) {
		MessageBox(NULL, "Could not inject dll into the game process. Please try launching the game again.", "Fatal error", MB_ICONERROR);
		TerminateProcess(processInformation.hProcess, 0);
		return 0;
	}

	ResumeThread(processInformation.hThread);
	return 1;
}

/* EOF */