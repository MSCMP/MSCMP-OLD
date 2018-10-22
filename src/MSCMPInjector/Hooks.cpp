#include <ctype.h>
#include <cstring>
#include <assert.h>

#include "MonoLoader.h"
#include "MemFunctions.h"

extern ptrdiff_t CustomLogCallbackAddress;

void Injector_SetupLogAndTracing();
void Injector_SetupDebugger();
void Injector_RunMP();

extern "C" {
ptrdiff_t GetScriptingManager_Address = 0;
ptrdiff_t HookInit_ReturnAddress = 0;
ptrdiff_t HookGiveChanceToAttachDebugger_ReturnAddress = 0;
ptrdiff_t sub_1401736F0_Address = 0;
ptrdiff_t sub_1401F8FA0_Address = 0;

void HookInitProc();
void HookGiveChanceToAttachDebuggerProc();

void HookInit()
{
	Injector_SetupLogAndTracing();
	Injector_RunMP();
}

void SetupDebugger()
{
	Injector_SetupDebugger();
}
}

/**
 * Install hooks for X64 version of the launcher.
 */
void _cdecl InstallHooks(ptrdiff_t moduleAddress)
{
	const ptrdiff_t baseAddress = moduleAddress - 0x140000000;

	// Install init hook when we will run the Mono DLL of the mod.

	GetScriptingManager_Address = baseAddress + 0x000000014017DB90;
	HookInit_ReturnAddress = baseAddress + 0x00000001402BCD72;
	InstallJmpHook(baseAddress + 0x00000001402BCD60, HookInitProc);

	// Install command line init hook used to install debugger.

	HookGiveChanceToAttachDebugger_ReturnAddress = baseAddress + 0x00000001401739A6;
	sub_1401736F0_Address = baseAddress + 0x00000001401736F0;
	sub_1401F8FA0_Address = baseAddress + 0x00000001401F8FA0;
	InstallJmpHook(baseAddress + 0x0000000140173996, HookGiveChanceToAttachDebuggerProc);

	// And setup rest of the memory addresses.

	CustomLogCallbackAddress = baseAddress +  0x000000014106FF58;

}

/* eof */
