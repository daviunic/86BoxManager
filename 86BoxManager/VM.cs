using System;

namespace _86boxManager
{
    [Serializable] //For serializing VMs so they can be stored in the registry
    public class VM
    {
        public IntPtr hWnd { get; set; } //Window handle for the VM once it's started
        public string Name { get; set; } //Name of the virtual machine
        public string Desc { get; set; } //Description
        public string Path { get; set; } //Path to config, nvr, etc.
        public int Status { get; set; } //Status
        public int Pid { get; set; } //Process ID of 86box.exe running the VM

        // I will make this an enum at some point - starfrost013
        public const int STATUS_STOPPED = 0; //VM is not running
        public const int STATUS_RUNNING = 1; //VM is running
        public const int STATUS_WAITING = 2; //VM is waiting for user response
        public const int STATUS_PAUSED = 3; //VM is paused

        public VM()
        {
            Name = "defaultName";
            Desc = "defaultDesc";
            Path = "defaultPath";
            Status = STATUS_STOPPED;
            hWnd = IntPtr.Zero;
        }

        public VM(string name, string desc, string path)
        {
            Name = name;
            Desc = desc;
            Path = path;
            Status = STATUS_STOPPED;
            hWnd = IntPtr.Zero;
        }

        // wut
        // entirely redundant
        public override string ToString()
        {
            return "Name: " + Name + ", description: " + Desc + ", path: " + Path + ", status: " + Status;
        }

        //Returns a lovely status string for use in UI
        public string GetStatusString()
        {
            switch (Status)
            {
                case STATUS_STOPPED: return "Stopped";
                case STATUS_RUNNING: return "Running";
                case STATUS_PAUSED: return "Paused";
                case STATUS_WAITING: return "Waiting";
                default: return "Invalid status";
            }
        }
    }
}
