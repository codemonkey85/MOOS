﻿using Internal.Runtime.CompilerHelpers;
using Kernel;
using Kernel.Driver;
using Kernel.NET;
using System;
using System.Runtime;
using System.Net;

unsafe class Program
{
    static void Main() { }

    /*
     * Minimum system requirement:
     * 64MiB of RAM
     * Memory Map:
     * 0x100000 - 0xEFFFFF -> Load data
     * 0x1000000 - 0x2000000 -> System
     * 0x2000000 - ∞ -> Free to use
     */
    [RuntimeExport("Main")]
    static void Main(MultibootInfo* Info,IntPtr Mod)
    {
        #region Initializations
        if (Mod == IntPtr.Zero) 
        {
            byte* ImageBase = (byte*)0x110000; //Do not modify it
            DOSHeader* doshdr = (DOSHeader*)ImageBase;
            NtHeaders64* nthdr = (NtHeaders64*)(ImageBase + doshdr->e_lfanew);
            SectionHeader* sections = ((SectionHeader*)(ImageBase + doshdr->e_lfanew + sizeof(NtHeaders64)));
            IntPtr moduleSeg = IntPtr.Zero;
            for (int i = 0; i < nthdr->FileHeader.NumberOfSections; i++)
            {
                if (*(ulong*)sections[i].Name == 0x73656C75646F6D2E) moduleSeg = (IntPtr)(nthdr->OptionalHeader.ImageBase + sections[i].VirtualAddress);
                Native.Stosb((void*)(nthdr->OptionalHeader.ImageBase + sections[i].VirtualAddress), 0, sections[i].SizeOfRawData);
                Native.Movsb((void*)(nthdr->OptionalHeader.ImageBase + sections[i].VirtualAddress), ImageBase + sections[i].PointerToRawData, sections[i].SizeOfRawData);
            }
            delegate*<MultibootInfo*, IntPtr, void> p = (delegate*<MultibootInfo*, IntPtr, void>)(nthdr->OptionalHeader.ImageBase + nthdr->OptionalHeader.AddressOfEntryPoint);
            p(Info, moduleSeg);
            return;
        }
        

        for (uint i = 1024 * 1024 * 32; i < 1024 * 1024 * 512; i += 1024 * 1024)
        {
            Allocator.AddFreePages((System.IntPtr)(i), 256);
        }

        StartupCodeHelpers.InitializeRuntime(Mod);
        #endregion

        PageTable.Initialise();
        VBE.Initialise((VBEInfo*)Info->VBEInfo);
        Console.Setup();
        IDT.Disable();
        GDT.Initialise();
        IDT.Initialise();
        IDT.Enable();
        Serial.Initialise();
        PCI.Initialise();
        PIT.Initialise();
        PS2Mouse.Initialise();
        ACPI.Initialize();
        SMBIOS.Initialise();

        Serial.WriteLine("Hello World");
        Console.WriteLine("Hello, World!");
        Console.WriteLine("Use Native AOT (Core RT) Technology.");

        /*
        ARP.Initialise();
        Network.Initialise(IPAddress.Parse(192, 168, 137, 188), IPAddress.Parse(192, 168, 137, 1));
        RTL8139.Initialise();
        ARP.Require(Network.Gateway);

        for (; ; );
        */

        PIT.Wait(1000);

        VBEInfo* vbe = (VBEInfo*)Info->VBEInfo;
        if (vbe->PhysBase != 0)
        {
            Framebuffer.VideoMemory = (uint*)vbe->PhysBase;
            Framebuffer.SetVideoMode(vbe->ScreenWidth, vbe->ScreenHeight);
        }
        else 
        {
            Framebuffer.Setup();
            Framebuffer.SetVideoMode(800, 600);
        }
        Framebuffer.TripleBuffered = true;

        int[] cursor = new int[]
            {
                1,0,0,0,0,0,0,0,0,0,0,0,
                1,1,0,0,0,0,0,0,0,0,0,0,
                1,2,1,0,0,0,0,0,0,0,0,0,
                1,2,2,1,0,0,0,0,0,0,0,0,
                1,2,2,2,1,0,0,0,0,0,0,0,
                1,2,2,2,2,1,0,0,0,0,0,0,
                1,2,2,2,2,2,1,0,0,0,0,0,
                1,2,2,2,2,2,2,1,0,0,0,0,
                1,2,2,2,2,2,2,2,1,0,0,0,
                1,2,2,2,2,2,2,2,2,1,0,0,
                1,2,2,2,2,2,2,2,2,2,1,0,
                1,2,2,2,2,2,2,2,2,2,2,1,
                1,2,2,2,2,2,2,1,1,1,1,1,
                1,2,2,2,1,2,2,1,0,0,0,0,
                1,2,2,1,0,1,2,2,1,0,0,0,
                1,2,1,0,0,1,2,2,1,0,0,0,
                1,1,0,0,0,0,1,2,2,1,0,0,
                0,0,0,0,0,0,1,2,2,1,0,0,
                0,0,0,0,0,0,0,1,2,2,1,0,
                0,0,0,0,0,0,0,1,2,2,1,0,
                0,0,0,0,0,0,0,0,1,1,0,0
            };

        for (; ; )
        {
            Framebuffer.Clear(0x0);
            ASC16.DrawString("FPS: ", 10, 10, 0xFFFFFFFF);
            ASC16.DrawString(((ulong)FPSMeter.FPS).ToString(), 42, 10, 0xFFFFFFFF);
            DrawCursor(cursor, PS2Mouse.X, PS2Mouse.Y);
            Framebuffer.Update();
            FPSMeter.Update();
        }
    }

    private static void DrawCursor(int[] cursor, int x, int y)
    {
        for (int h = 0; h < 21; h++)
            for (int w = 0; w < 12; w++)
            {
                if (cursor[h * 12 + w] == 1)
                    Framebuffer.DrawPoint(w + x, h + y, 0xFFFFFFFF);

                if (cursor[h * 12 + w] == 2)
                    Framebuffer.DrawPoint(w + x, h + y, 0x0);
            }
    }
}
