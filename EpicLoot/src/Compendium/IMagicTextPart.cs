namespace EpicLoot.Compendium;

public interface IMagicTextPart
{
    bool IsMatch(string loweredQuery);
    void Enable(bool enable);
    void Destroy();
    float GetHeight();
}
