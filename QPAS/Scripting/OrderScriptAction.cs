using EntityModel;

namespace QPAS.Scripting
{
    public abstract class OrderScriptAction
    {
    }

    public class SetTrade : OrderScriptAction
    {
        public SetTrade(Order order, Trade trade)
        {
            Order = order;
            Trade = trade;
        }

        public Order Order { get; }
        public Trade Trade { get; }

        public override string ToString()
        {
            return "Set Trade: " + Order.ToString() + " Set To " + Trade.Name;
        }
    }

    public class CreateTrade : OrderScriptAction
    {
        public CreateTrade(Trade trade)
        {
            Trade = trade;
        }

        public Trade Trade { get; }

        public override string ToString()
        {
            return "Create trade: " + Trade.Name;
        }
    }
}