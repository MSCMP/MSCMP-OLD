; Unity player hooks for MSCMP.

extern HookInit: PROC
extern SetupDebugger: PROC

extern GetScriptingManager_Address: qword
extern HookInit_ReturnAddress: qword
extern sub_1401736F0_Address: qword
extern sub_1401F8FA0_Address: qword
extern HookGiveChanceToAttachDebugger_ReturnAddress: qword

.code

HookInitProc PROC
	; mod code

	; stack is not 16bytes aligned here do it for our call and then rewind the stack pointer
	; in this method rbp is not used so we can safely misuse it to store the stack ptr

	mov	rbp, rsp
	sub	rsp, 100h
	and     rsp, 0FFFFFFFFFFFFFFF0h
	call    HookInit
	mov	rsp, rbp


	; call the original code

	mov     [rsp+8], rbx
	push    rdi
	sub     rsp, 140h
	call    GetScriptingManager_Address

	jmp	HookInit_ReturnAddress

HookInitProc ENDP

HookGiveChanceToAttachDebuggerProc PROC
	; mod code

	; stack is 16-bytes aligned at this moment so we don't have to do any additional magic here
	call    SetupDebugger

	; call the original code

	; skip first 'GiveChanceToAttachDebugger'
	; call    sub_1401F8FA0_Address

	mov     rdx, rbx
	mov     rcx, r12
	call    sub_1401736F0_Address

	jmp	HookGiveChanceToAttachDebugger_ReturnAddress

HookGiveChanceToAttachDebuggerProc ENDP

End
