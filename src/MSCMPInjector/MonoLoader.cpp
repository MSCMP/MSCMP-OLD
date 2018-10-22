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

#define MONOFUNC(name)\
	name = GetFunctionAddress<name##_t>(#name);\
	if (!name)\
	{\
		MessageBox(NULL, #name, "Failed to get function address.", NULL); \
		return false;\
	}

	MONOFUNC(mono_get_root_domain);
	MONOFUNC(mono_thread_attach);
	MONOFUNC(mono_unity_set_vprintf_func);
	MONOFUNC(mono_class_from_name);
	MONOFUNC(mono_class_get_method_from_name);
	MONOFUNC(mono_runtime_invoke);
	MONOFUNC(mono_domain_assembly_open);
	MONOFUNC(mono_assembly_get_image);
	MONOFUNC(mono_domain_get);
	MONOFUNC(mono_thread_get_main);
	MONOFUNC(mono_jit_parse_options);
	MONOFUNC(mono_debug_init);
	MONOFUNC(mono_set_commandline_arguments);
	MONOFUNC(mono_add_internal_call);
	MONOFUNC(mono_string_to_utf8);
	MONOFUNC(g_free);
	MONOFUNC(mono_print_unhandled_exception);
	MONOFUNC(mono_trace_set_level_string);
	MONOFUNC(mono_trace_set_mask_string);
#undef MONOFUNC

	return true;
}