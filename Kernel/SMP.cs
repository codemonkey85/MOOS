﻿using MOOS.Driver;
using MOOS.Misc;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace MOOS
{
    public static unsafe class SMP
    {
        //https://wiki.osdev.org/Memory_Map_(x86)
        public const ulong NumActivedProcessors = 0x50000;
        public const ulong APMain = 0x50008;
        public const ulong Stacks = 0x50016;
        public const ulong SharedGDT = 0x50024;
        public const ulong SharedIDT = 0x50032;
        public const ulong SharedPageTable = 0x51000;
        public const ulong Trampoline = 0x60000;

        public static Queue<ThreadFor> WorkGroups;

        public class ThreadFor 
        {
            public volatile uint For;
            public volatile delegate*<void> Method;
        }

        public static void RunOnAnyCPU(delegate*<void> method)
        {
            if(WorkGroups == null) 
            {
                new Thread(method).Start();
            }
            else
            {
                WorkGroups.Enqueue(new ThreadFor() { For = LastFreeCPUIndex, Method = method });
            }
        }

        public volatile static int NumFreeCPU;
        public static int NumCPU { get => ACPI.LocalAPIC_CPUIDs.Count; }

        public static volatile uint LastFreeCPUIndex;

        public static delegate*<void> Take() 
        {
            NumFreeCPU++;
            while (WorkGroups.Tail == null || WorkGroups.Tail.For != ThisCPU)
            {
                LastFreeCPUIndex = ThisCPU;
                Native._pause();
            }

            ThreadFor tf = WorkGroups.Dequeue();
            var addr = tf.Method;
            tf.Dispose();
            NumFreeCPU--;
            return addr; 
        }

        public static uint ThisCPU => LocalAPIC.GetId();


        //Method for other CPU cores
        //GDT, IDT, PageTable has been configured in Trampoline. so we don't need to set it here
        public static void ApplicationProcessorMain(int Core)
        {
            SSE.enable_sse();
            LocalAPIC.Initialize();
            //Console.WriteLine("Hello from Application Processor");
            for (; ; ) SMP.Take()();
        }

        public static void Initialize(uint trampoline)
        {
            if (ThisCPU != 0) Panic.Error("Error: Bootstrap CPU is not CPU 0");

            ushort* activedProcessor = (ushort*)NumActivedProcessors;
            *activedProcessor = 1;

            ulong* apMain = (ulong*)APMain;
            *apMain = (ulong)(delegate*<int,void>)&ApplicationProcessorMain;

            ulong* stacks = (ulong*)Stacks;
            *stacks = (ulong)Allocator.Allocate((ulong)(ACPI.LocalAPIC_CPUIDs.Count * 1048576));

            fixed(GDT.GDTDescriptor* gdt = &GDT.gdtr) 
            {
                ulong* sgdt = (ulong*)SharedGDT;
                *sgdt = (ulong)gdt;
            }

            fixed (IDT.IDTDescriptor* idt = &IDT.idtr)
            {
                ulong* sidt = (ulong*)SharedIDT;
                *sidt = (ulong)idt;
            }

            WorkGroups = new Queue<ThreadFor>();
            NumFreeCPU = 0;

            for (int i = 0; i < NumCPU; ++i)
            {
                uint id = ACPI.LocalAPIC_CPUIDs[i]; 
                Console.WriteLine($"Starting CPU{id.ToString()}");
                if (id != ThisCPU)
                {
                    int last = *activedProcessor;
                    LocalAPIC.SendInit(id); 
                    LocalAPIC.SendStartup(id, (trampoline >> 12));
                    while (last == *activedProcessor) Native._pause();
                }
            }
            Console.WriteLine("All CPUs started");
        }
    }
}
