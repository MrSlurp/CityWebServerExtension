using System;

namespace CWS_MrSlurpExtensions
{
    public class PopulationGroup
    {
        public String Name { get; set; }

        public int Amount { get; set; }

        public PopulationGroup()
        {
        }

        public PopulationGroup(String name, int amount)
        {
            Name = name;
            Amount = amount;
        }
    }
}