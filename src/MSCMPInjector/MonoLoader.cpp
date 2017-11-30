#include "MonoLoader.h"

Mono::~Mono()
{
	if (MonoDllModule) {
		FreeLibrary(MonoDllModule);
		MonoDllModule = nullptr;
	}
}


bool Mono::Setup(const char *MonoDllPath)
{
	MonoDllModule = LoadLibraryA(MonoDllPath);
	if (!MonoDllModule)
	{
		return false;
	}

#define SETUP_FUNCTION(name)\
	name = GetFunctionAddress<name##_t>(#name);\
	if (!name)\
	{\
		return false;\
	}

	SETUP_FUNCTION(mono_get_root_domain);
	SETUP_FUNCTION(mono_thread_attach);
	SETUP_FUNCTION(mono_unity_set_vprintf_func);
	SETUP_FUNCTION(mono_class_from_name);
	SETUP_FUNCTION(mono_class_get_method_from_name);
	SETUP_FUNCTION(mono_runtime_invoke);
	SETUP_FUNCTION(mono_domain_assembly_open);
	SETUP_FUNCTION(mono_assembly_get_image);
	SETUP_FUNCTION(mono_domain_get);
	SETUP_FUNCTION(mono_thread_get_main);

#undef SETUP_FUNCTION

	return true;
}