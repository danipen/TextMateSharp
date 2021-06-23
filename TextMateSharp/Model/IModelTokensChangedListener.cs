namespace TextMateSharp.Model
{
    public interface IModelTokensChangedListener
    {
        void ModelTokensChanged(ModelTokensChangedEvent e);
    }
}
