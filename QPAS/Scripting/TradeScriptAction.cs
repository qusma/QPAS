using EntityModel;

namespace QPAS.Scripting
{
    public abstract class TradeScriptAction
    {
    }

    public class SetTag : TradeScriptAction
    {
        public SetTag(Trade trade, Tag tag)
        {
            Trade = trade;
            Tag = tag;
        }

        public Trade Trade { get; }
        public Tag Tag { get; }

        public override string ToString()
        {
            return "Set Tag: " + Tag.Name + " On Trade: " + Trade.Name;
        }
    }

    public class SetStrategy : TradeScriptAction
    {
        public SetStrategy(Trade trade, Strategy strategy)
        {
            Trade = trade;
            Strategy = strategy;
        }

        public Trade Trade { get; }
        public Strategy Strategy { get; }

        public override string ToString()
        {
            return "Set Strategy: " + Strategy.Name + " On Trade: " + Trade.Name;
        }
    }

    public class CloseTrade : TradeScriptAction
    {
        public CloseTrade(Trade trade)
        {
            Trade = trade;
        }

        public Trade Trade { get; }

        public override string ToString()
        {
            return "Close Trade: " + Trade.Name;
        }
    }
}