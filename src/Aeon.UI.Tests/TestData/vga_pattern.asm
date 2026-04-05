; VGA Mode 13h Pattern Program
; Produces a 320x200 256-color gradient image then exits.
; Assembled as a DOS .COM flat binary (loaded at CS:0100h).
;
; Usage: nasm -f bin -o vga_pattern.com vga_pattern.asm

org 0x100

    ; Set VGA mode 13h (320x200, 256 colors)
    mov ax, 0x0013
    int 0x10

    ; Point ES to VGA framebuffer segment A000h
    mov ax, 0xA000
    mov es, ax

    ; Fill every pixel (320*200 = 64000 bytes) with a gradient.
    ; The low byte of the loop counter produces a repeating 0-255 ramp.
    xor di, di              ; DI = 0 (destination offset)
    mov cx, 64000           ; 64000 pixels
.fill:
    mov al, cl              ; colour = low byte of remaining count
    stosb                   ; ES:[DI++] = AL
    loop .fill

    ; Terminate cleanly via DOS INT 21h / AH=4Ch
    mov ax, 0x4C00
    int 0x21
