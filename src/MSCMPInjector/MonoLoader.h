#pragma once

#include <Windows.h>
#include <assert.h>

#include <mono/jit/jit.h>
#include <mono/metadata/assembly.h>
#include <mono/metadata/mono-debug.h>

#define CONV __fastcall

typedef MonoDomain* (CONV* mono_get_root_domain_t)();
typedef MonoThread* (CONV* mono_thread_attach_t)(MonoDomain* mDomain);
typedef size_t (CONV *print_fn_t)(const char *, va_list);
typedef void (CONV *mono_unity_set_vprintf_func_t)(print_fn_t fn);
typedef MonoClass* (CONV* mono_class_from_name_t)(MonoImage* image, const char* name_space, const char* name);
typedef MonoMethod* (CONV* mono_class_get_method_from_name_t)(MonoClass* mclass, const char* name, size_t param_count);
typedef MonoObject* (CONV* mono_runtime_invoke_t)(MonoMethod* method, void* obj, void** params, MonoObject** exc);
typedef MonoAssembly* (CONV* mono_domain_assembly_open_t)(MonoDomain* mDomain, const char* filepath);
typedef MonoImage* (CONV* mono_assembly_get_image_t)(MonoAssembly *assembly);
typedef MonoDomain* (CONV* mono_domain_get_t)();
typedef MonoThread* (CONV* mono_thread_get_main_t)();
typedef void (CONV *mono_jit_parse_options_t)(int argc, char * argv[]);
typedef void (CONV *mono_debug_init_t)(MonoDebugFormat format);
typedef void (CONV *mono_set_commandline_arguments_t)(int a1, char **a2, char *a3);
typedef void (CONV *mono_add_internal_call_t)(const char *name, const void* method);
typedef char* (CONV *mono_string_to_utf8_t)(MonoString *string_obj);
typedef void (CONV *g_free_t)(void *data);
typedef void (CONV *mono_print_unhandled_exception_t)(MonoObject *exception);
typedef void (CONV *mono_trace_set_level_string_t)(const char *level);
typedef int (CONV *mono_trace_set_mask_string_t)(const char *mask);

class Mono
{
private:

	HMODULE						MonoDllModule			= nullptr;

	template <typename FUNCTION_TYPE_T>
	FUNCTION_TYPE_T	GetFunctionAddress(const char *FunctionName) const
	{
		assert(MonoDllModule);
		return static_cast<FUNCTION_TYPE_T>(static_cast<void*>(GetProcAddress(MonoDllModule, FunctionName)));
	}

public:
				~Mono			();

	bool		Setup			(const char *MonoDllPath);


#define MONOFUNC(name)\
	name##_t name = nullptr

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
	MONOFUNC(g_free); // in unity mono.dll mono_free it's called g_free
	MONOFUNC(mono_print_unhandled_exception);
	MONOFUNC(mono_trace_set_level_string);
	MONOFUNC(mono_trace_set_mask_string);

#undef MONOFUNC
};
