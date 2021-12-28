namespace DaJet.Studio.UI
{
    public interface IListViewModelController
    {
        void AddNewItem();
        void EditItem(object item);
        void CopyItem(object item);
        void RemoveItem(object item);
    }
}