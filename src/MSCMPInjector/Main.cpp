#include <Windows.h>
#include <process.h>

#include "MonoLoader.h"

Mono mono;

void GetModulePath(HMODULE module, char *path)
{
	GetModuleFileName(module, path, MAX_PATH);
	size_t pathLen = strlen(path);
	for (size_t i = pathLen - 1; i > 0; --i) {
		if (path[i] == '\\') {
			path[i] = 0;
			break;
		}
	}
}

bool RunMP(const char *clientDllPath)
{
	MonoDomain* domain = mono.mono_domain_get();
	if (! domain)
	{
		return false;
	}

	MonoAssembly* domainassembly = mono.mono_domain_assembly_open(domain, clientDllPath);
	if (!domainassembly)
	{
		return false;
	}

	MonoImage* image = mono.mono_assembly_get_image(domainassembly);
	if (!image)
	{
		return false;
	}

	MonoClass* monoClass = mono.mono_class_from_name(image, "MSCMP", "Client");
	if (!monoClass)
	{
		return false;
	}

	MonoMethod* monoClassMethod = mono.mono_class_get_method_from_name(monoClass, "Start", 0);
	if (!monoClassMethod)
	{
		return false;
	}

	// As there is no 'gold method' of verifying that the call succeeded (at least not documented).
	// We trust it and just invoke the method.

	mono.mono_runtime_invoke(monoClassMethod, nullptr, nullptr, nullptr);
	return true;
}

char monoDllPath[MAX_PATH] = { 0 };
char ClientDllPath[MAX_PATH] = { 0 };

void SetupMSCMP()
{
	if (!mono.Setup(monoDllPath))
	{
		MessageBox(NULL, "Unable to setup mono loader!", "MSCMP", MB_ICONERROR);
		ExitProcess(0);
		return;
	}


	// First of all attach logger so we will know what happens when mono does not like what we send to it.

	mono.mono_unity_set_vprintf_func([](const char *message, va_list args) -> int
	{
		char full_message[2048] = { 0 };
		vsprintf(full_message, message, args);
		va_end(args);
		MessageBox(NULL, full_message, "MSCMP", NULL);

		return 1;
	});

	//MonoDomain *rootDomain = mono.mono_get_root_domain();
	//mono.mono_thread_attach(rootDomain);

	if (!RunMP(ClientDllPath))
	{
		MessageBox(NULL, "Failed to run multiplayer mod!", "MSCMP", MB_ICONERROR);
		ExitProcess(0);
	}
}

typedef int (*sub_6596C0_t)();
sub_6596C0_t sub_6596C0 = 0;

int _stdcall InitHook()
{
	int result = sub_6596C0();
	SetupMSCMP();
	return result;
}

BOOL WINAPI DllMain(HMODULE hModule, unsigned Reason, void *Reserved)
{
	switch (Reason) {
	case DLL_PROCESS_ATTACH:
		DisableThreadLibraryCalls(hModule);

		// Make sure we have mono dll to work with.


		GetModulePath(GetModuleHandle(0), monoDllPath);
		strcat(monoDllPath, "\\mysummercar_Data\\Mono\\mono.dll");

		if (GetFileAttributes(monoDllPath) == INVALID_FILE_ATTRIBUTES)
		{
			MessageBox(NULL, "Unable to find mono.dll!", "MSCMP", MB_ICONERROR);
			ExitProcess(0);
			return FALSE;
		}

		// Now make sure we have client file. Do it here so we will not do any redundant processing.

		GetModulePath(GetModuleHandle("MSCMPInjector.dll"), ClientDllPath);
		strcat(ClientDllPath, "\\MSCMPClient.dll");

		if (GetFileAttributes(ClientDllPath) == INVALID_FILE_ATTRIBUTES)
		{
			MessageBox(NULL, "Unable to find MSC MP Client files!", "MSCMP", MB_ICONERROR);
			ExitProcess(0);
			return FALSE;
		}

		unsigned baseAddress = (unsigned)(GetModuleHandle(NULL)) - 0x400000;
		sub_6596C0 = (sub_6596C0_t)(baseAddress + 0x006596C0);

		// Install initialization hook.

		unsigned hookAddress = baseAddress + 0x0065C2AE;

		unsigned hookSize = 5;
		DWORD oldProtection = NULL;
		VirtualProtect((void *)hookAddress, hookSize, PAGE_EXECUTE_READWRITE, &oldProtection);

		unsigned char *hookPlace = (unsigned char *)(hookAddress);
		*hookPlace = 0xE8;
		unsigned *address = (unsigned *)(hookPlace + 1);
		*address = ((unsigned)InitHook - (hookAddress + hookSize));
		break;
	}
	return TRUE;
}
