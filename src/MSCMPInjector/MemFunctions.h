#pragma once

#include <cstddef>
#include <Windows.h>

/**
 * Writes value of given type to writeable memory at given address.
 *
 * @pre @a where memory must be writeable.
 * @param[in] where Address where to write value.
 * @param[in] value The value to write.
 */
template <typename TYPE>
inline void WriteValue(ptrdiff_t where, TYPE value)
{
	*reinterpret_cast<TYPE  *>(where) = value;
}

class UnprotectedMemoryScope
{
private:
	DWORD old_protection = 0;
	void *where = nullptr;
	size_t size = 0;
public:
	UnprotectedMemoryScope(ptrdiff_t where, size_t size);
	~UnprotectedMemoryScope();
};

void InstallJmpHook(ptrdiff_t where, ptrdiff_t func_address);

template <typename FUNC_T>
void InstallJmpHook(ptrdiff_t where, FUNC_T func)
{
	InstallJmpHook(where, reinterpret_cast<ptrdiff_t>(func));
}

/* eof */
