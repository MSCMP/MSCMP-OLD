#include <Windows.h>
#include <process.h>

#include "MonoLoader.h"
#include "MemFunctions.h"

#include <exception>

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


static void ShowMessageBox(MonoString *message, MonoString *title)
{
	if (message && title)
	{
		char *messageText = mono.mono_string_to_utf8(message);
		char *titleText = mono.mono_string_to_utf8(title);

		if (messageText && titleText)
		{
			MessageBox(NULL, messageText, titleText, NULL);
		}

		mono.g_free(messageText);
		mono.g_free(titleText);
	}
}

FILE *unityLog = nullptr;

bool RunMP(const char *clientDllPath)
{
	// Register our internal calls.

	mono.mono_add_internal_call ("MSCMP.Client::ShowMessageBox", ShowMessageBox);

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

char MonoDllPath[MAX_PATH] = { 0 };
char ClientDllPath[MAX_PATH] = { 0 };

/**
 * Custom unity log handler.
 */
int _cdecl UnityLog(int a1, const char *message, va_list args)
{
	fprintf(unityLog, "[%i] ", a1);
	vfprintf(unityLog, message, args);
	va_end(args);
#ifdef _DEBUG
	OutputDebugString(message);
	fflush(unityLog);
#endif

	return 0;
}

void Injector_SetupLogAndTracing()
{
	mono.mono_unity_set_vprintf_func([](const char *message, va_list args) -> size_t
	{
		UnityLog(666, message, args);
		return 1;
	});
	mono.mono_trace_set_level_string("debug");
	mono.mono_trace_set_mask_string("all");

}
void Injector_SetupDebugger()
{
	if (!getenv("UNITY_GIVE_CHANCE_TO_ATTACH_DEBUGGER"))
	{
		return;
	}

	const char *argv[] = {
		"--debugger-agent=transport=dt_socket,embedding=1,server=y,address=127.0.0.1:56000,defer=y"
	};
	mono.mono_jit_parse_options(1, const_cast<char **>(argv));
	mono.mono_debug_init(MONO_DEBUG_FORMAT_MONO);
}

void Injector_RunMP()
{
	if (!RunMP(ClientDllPath))
	{
		MessageBox(NULL, "Failed to run multiplayer mod!", "MSCMP", MB_ICONERROR);
		ExitProcess(0);
	}
}

/** Memory address where unit custom log callback should be set. */
extern ptrdiff_t CustomLogCallbackAddress = 0;

/**
 * Method used to install architecture dependent hooks.
 *
 * It's implementation can be found in HooksX86.cpp or HooksX64.cpp files.
 */
void InstallHooks(ptrdiff_t moduleAddress);


/**
 * Handles fatal error.
 *
 * @param[in] message The error message.
 */
static void FatalError(const char *const message)
{
	MessageBox(NULL, "Unable to create Unity Log!", "MSCMP", MB_ICONERROR);
	ExitProcess(0);
}


/**
 * The injector DLL entry point.
 */
BOOL WINAPI DllMain(HMODULE hModule, unsigned Reason, void *Reserved)
{
	switch (Reason) {
	case DLL_PROCESS_ATTACH:
	{
		DisableThreadLibraryCalls(hModule);

		// Setup unity log hook.

		char UnityLogPath[MAX_PATH] = { 0 };
		GetModulePath(GetModuleHandle("MSCMPInjector.dll"), UnityLogPath);
		strcat(UnityLogPath, "\\unityLog.txt");

		unityLog = fopen(UnityLogPath, "w+");
		if (!unityLog)
		{
			FatalError("Unable to create Unity Log!");
			return FALSE;
		}

		// Make sure we have mono dll to work with.

		GetModulePath(GetModuleHandle(0), MonoDllPath);
		strcat(MonoDllPath, "\\mysummercar_Data\\Mono\\mono.dll");

		if (GetFileAttributes(MonoDllPath) == INVALID_FILE_ATTRIBUTES)
		{
			FatalError("Unable to find mono.dll!");
			return FALSE;
		}

		// Now make sure we have client file. Do it here so we will not do any redundant processing.

		GetModulePath(GetModuleHandle("MSCMPInjector.dll"), ClientDllPath);
		strcat(ClientDllPath, "\\MSCMPClient.dll");

		if (GetFileAttributes(ClientDllPath) == INVALID_FILE_ATTRIBUTES)
		{
			FatalError("Unable to find mod client files!");
			return FALSE;
		}

		if (!mono.Setup(MonoDllPath))
		{
			FatalError("Unable to setup mono loader!");
			return FALSE;
		}

		const ptrdiff_t moduleAddress = reinterpret_cast<ptrdiff_t>(GetModuleHandle(NULL));
		InstallHooks(moduleAddress);

		// Common memory operations:

		// Set custom log callback.

		UnprotectedMemoryScope unprotect(CustomLogCallbackAddress, sizeof(ptrdiff_t));
		WriteValue<ptrdiff_t>(CustomLogCallbackAddress, reinterpret_cast<ptrdiff_t>(UnityLog));
	}
	break;

	case DLL_PROCESS_DETACH:
		if (unityLog)
		{
			fclose(unityLog);
			unityLog = nullptr;
		}
		break;
	}
	return TRUE;
}
