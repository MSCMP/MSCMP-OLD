#pragma once

#include <Windows.h>
#include <assert.h>

#include <mono/jit/jit.h>
#include <mono/metadata/assembly.h>
#include <mono/metadata/mono-debug.h>

typedef MonoDomain* (__cdecl* mono_get_root_domain_t)();
typedef MonoThread* (__cdecl* mono_thread_attach_t)(MonoDomain* mDomain);
typedef int (__cdecl *print_fn_t)(const char *, va_list);
typedef int (__cdecl *mono_unity_set_vprintf_func_t)(print_fn_t fn);
typedef MonoClass* (__cdecl* mono_class_from_name_t)(MonoImage* image, const char* name_space, const char* name);
typedef MonoMethod* (__cdecl* mono_class_get_method_from_name_t)(MonoClass* mclass, const char* name, int param_count);
typedef MonoObject* (__cdecl* mono_runtime_invoke_t)(MonoMethod* method, void* obj, void** params, MonoObject** exc);
typedef MonoAssembly* (__cdecl* mono_domain_assembly_open_t)(MonoDomain* mDomain, const char* filepath);
typedef MonoImage* (__cdecl* mono_assembly_get_image_t)(MonoAssembly *assembly);
typedef MonoDomain* (__cdecl* mono_domain_get_t)();
typedef MonoThread* (_cdecl* mono_thread_get_main_t)();
typedef void (_cdecl *mono_jit_parse_options_t)(int argc, char * argv[]);
typedef void (_cdecl *mono_debug_init_t)(MonoDebugFormat format);
typedef void (_cdecl *mono_set_commandline_arguments_t)(int a1, char **a2, char *a3);
typedef void (_cdecl *mono_add_internal_call_t)(const char *name, const void* method);
typedef char* (_cdecl *mono_string_to_utf8_t)(MonoString *string_obj);
typedef void (_cdecl *g_free_t)(void *data);


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


	mono_get_root_domain_t		mono_get_root_domain	= nullptr;
	mono_thread_attach_t		mono_thread_attach		= nullptr;
	mono_unity_set_vprintf_func_t mono_unity_set_vprintf_func = nullptr;
	mono_class_from_name_t		mono_class_from_name	 = nullptr;
	mono_class_get_method_from_name_t mono_class_get_method_from_name = nullptr;
	mono_runtime_invoke_t		mono_runtime_invoke		= nullptr;
	mono_domain_assembly_open_t mono_domain_assembly_open = nullptr;
	mono_assembly_get_image_t	mono_assembly_get_image = nullptr;
	mono_domain_get_t			mono_domain_get			= nullptr;
	mono_thread_get_main_t		mono_thread_get_main	= nullptr;
	mono_jit_parse_options_t	mono_jit_parse_options	= nullptr;
	mono_debug_init_t			mono_debug_init			= nullptr;
	mono_set_commandline_arguments_t mono_set_commandline_arguments = nullptr;
	mono_add_internal_call_t	mono_add_internal_call = nullptr;
	mono_string_to_utf8_t		mono_string_to_utf8		= nullptr;
	g_free_t					g_free				= nullptr; // in unity mono.dll mono_free it's called g_free
};
