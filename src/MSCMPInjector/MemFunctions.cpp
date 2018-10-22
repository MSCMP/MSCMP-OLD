
#include "MemFunctions.h"

#include <assert.h>
#include <Windows.h>
#include <limits>

UnprotectedMemoryScope::UnprotectedMemoryScope(ptrdiff_t the_where, size_t the_size)
{
	size = the_size;
	where = reinterpret_cast<void *>(the_where);
	if (!VirtualProtect(where, size, PAGE_EXECUTE_READWRITE, &old_protection)) {
		assert(!"Failed to change protection state of the virtual memory block.");
	}
}

UnprotectedMemoryScope::~UnprotectedMemoryScope()
{
	DWORD dummy = 0;
	VirtualProtect(where, size, old_protection, &dummy);
	size = 0;
	where = nullptr;
	old_protection = 0;
}


static const unsigned char JUMP_CODE[] = {
	/*                            0x00  0x01  0x02  0x03  0x04  0x05  0x06  0x07  0x08  0x09 */
	/* push  rax               */ 0x50,
	/* mov   rax, address      */ 0x48, 0xb8, 0xcc, 0xcc, 0xcc, 0xcc, 0xcc, 0xcc, 0xcc, 0xcc,
	/* xchg  [rsp], rax        */ 0x48, 0x87, 0x04, 0x24,
	/* ret                     */ 0xc3
};

const size_t JUMP_CODE_SIZE = sizeof(JUMP_CODE);

void InstallJmpHook(ptrdiff_t where, ptrdiff_t func_address)
{
	const size_t ADDRESS_OFFSET = 3;
	const size_t ADDRESS_SIZE = 8;

	static_assert(sizeof(ptrdiff_t) == ADDRESS_SIZE, "ptrdiff_t size does not matches address size");
	void *const hookLocation = reinterpret_cast<void *>(where);

	UnprotectedMemoryScope unprotect(where, JUMP_CODE_SIZE);
	memcpy(hookLocation, JUMP_CODE, JUMP_CODE_SIZE);
	WriteValue<ptrdiff_t>(where + ADDRESS_OFFSET, func_address);
}
