namespace hook
{
    public class HookInstance
    {
        public readonly HookDefinition Definition;
        public IHookListener Host;
        public int Amount;
    }
}