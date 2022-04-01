namespace Gismo.Quip
{
    interface IInteractable
    {
        string GetInteractType();
        void OnSelected();
        void DoHighlight(bool status);

        bool CanInteract();
    }
}