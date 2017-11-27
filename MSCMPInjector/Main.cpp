#include <Windows.h>
#include <process.h>

#include <mono/jit/jit.h>
#include <mono/metadata/assembly.h>

HMODULE hMono = NULL;

//Simple function for returning mono functions.
DWORD GetMonoFunction(char* funcname) {

	return (DWORD)GetProcAddress(hMono, funcname);
}

typedef MonoDomain* (__cdecl* mono_root_domain_get_t)();
mono_root_domain_get_t mono_root_domain_get = NULL;
typedef MonoThread* (__cdecl* mono_thread_attach_t)(MonoDomain* mDomain);
mono_thread_attach_t mono_thread_attach = NULL;

void RunMP()
{
	//Class
	typedef MonoClass* (__cdecl* mono_class_from_name_t)(MonoImage* image, const char* name_space, const char* name);
	typedef MonoMethod* (__cdecl* mono_class_get_method_from_name_t)(MonoClass* mclass, const char* name, int param_count);
	mono_class_from_name_t mono_class_from_name = (mono_class_from_name_t)GetMonoFunction("mono_class_from_name");
	mono_class_get_method_from_name_t mono_class_get_method_from_name = (mono_class_get_method_from_name_t)GetMonoFunction("mono_class_get_method_from_name");

	//Code execution
	typedef MonoObject* (__cdecl* mono_runtime_invoke_t)(MonoMethod* method, void* obj, void** params, MonoObject** exc);
	mono_runtime_invoke_t mono_runtime_invoke = (mono_runtime_invoke_t)GetMonoFunction("mono_runtime_invoke");

	//Assembly
	typedef MonoAssembly* (__cdecl* mono_assembly_open_t)(MonoDomain* mDomain, const char* filepath);
	typedef MonoImage* (__cdecl* mono_assembly_get_image_t)(MonoAssembly *assembly);
	mono_assembly_open_t mono_assembly_open_ = (mono_assembly_open_t)GetMonoFunction("mono_domain_assembly_open");
	mono_assembly_get_image_t mono_assembly_get_image_ = (mono_assembly_get_image_t)GetMonoFunction("mono_assembly_get_image");

	//Now that we're attached we get the domain we are in.

	typedef MonoDomain* (__cdecl* mono_domain_get_t)();
	mono_domain_get_t mono_domain_getnormal = (mono_domain_get_t)GetMonoFunction("mono_domain_get");
	MonoDomain* domain = NULL;
	do {
		domain = mono_domain_getnormal();
	}
	while (! domain);

	//Opening a custom assembly in the domain.
	MonoAssembly* domainassembly = mono_assembly_open_(domain, "J:\\projects\\MSCMP\\MSCMP\\Debug\\MSCMPClient.dll");
	//Getting the assemblys Image(Binary image, essentially a file-module).
	MonoImage* Image = mono_assembly_get_image_(domainassembly);
	//Declaring the class inside the custom assembly we're going to use. (Image, NameSpace, ClassName)
	MonoClass* pClass = mono_class_from_name(Image, "MSCMP", "Client");
	//Declaring the method, that attaches our assembly to the game. (Class, MethodName, Parameters)
	MonoMethod* MonoClassMethod = mono_class_get_method_from_name(pClass, "Start", 0);
	//Invoking said method.
	mono_runtime_invoke(MonoClassMethod, NULL, NULL, NULL);


}

void SetupMSCMP()
{
	hMono = LoadLibraryA("J:\\Games\\steamapps\\common\\My Summer Car\\mysummercar_Data\\Mono\\mono.dll");
	if (!hMono)
	{
		ExitProcess(0);
		return;
	}

	// First of all attach logger so we will know what happends when mono does not like what we send to it.

	typedef int (__cdecl *print_fn_t)(const char *, va_list);
	typedef int (__cdecl *mono_unity_set_vprintf_func_t)(print_fn_t fn);
	mono_unity_set_vprintf_func_t mono_unity_set_vprintf_func_ = (mono_unity_set_vprintf_func_t)GetMonoFunction("mono_unity_set_vprintf_func");
	if (mono_unity_set_vprintf_func_) {

		mono_unity_set_vprintf_func_([](const char *message, va_list args) -> int
		{
			char full_message[2048] = { 0 };
			vsprintf(full_message, message, args);
			va_end(args);
			MessageBox(NULL, full_message, "MSCMP", NULL);

			return 1;
		});


	}
	mono_root_domain_get = (mono_root_domain_get_t)GetMonoFunction("mono_get_root_domain");
	mono_thread_attach = (mono_thread_attach_t)GetMonoFunction("mono_thread_attach");

	MonoDomain *root_domain = NULL;

	// Sleep a while so unit will initialize - this has to be done better with some hooks.
	Sleep(1000);

	do {
		root_domain = mono_root_domain_get();
	}
	while (! root_domain);

	mono_thread_attach(root_domain);

	RunMP();
	FreeLibrary(hMono);
}

UINT __stdcall ThreadMain(void*)
{
	SetupMSCMP();
	return 0;
}


BOOL WINAPI DllMain(HMODULE hModule, unsigned Reason, void *Reserved)
{
	switch (Reason) {
	case DLL_PROCESS_ATTACH:
		DisableThreadLibraryCalls(hModule);
		_beginthreadex(0, 0, ThreadMain, 0, 0, 0);
		break;
	}
	return TRUE;
}
