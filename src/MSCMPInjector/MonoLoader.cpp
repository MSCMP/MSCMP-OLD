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
		MessageBox(NULL, #name, "Failed to get function address.", NULL); \
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
	SETUP_FUNCTION(mono_jit_parse_options);
	SETUP_FUNCTION(mono_debug_init);
	SETUP_FUNCTION(mono_set_commandline_arguments);
	SETUP_FUNCTION(mono_add_internal_call);
	SETUP_FUNCTION(mono_string_to_utf8);
	SETUP_FUNCTION(g_free);

#undef SETUP_FUNCTION

	return true;
}