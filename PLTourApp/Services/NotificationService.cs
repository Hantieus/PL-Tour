namespace PLTourApp.Services;

public class NotificationService
{
    public event Action OnInterrupt;

    public void TriggerInterrupt()
    {
        OnInterrupt?.Invoke();
    }
}