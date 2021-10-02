namespace NVLenovoController.Features
{
    public interface IFeature<T>
    {
        T GetState();
        void SetState(T state);
    }
}