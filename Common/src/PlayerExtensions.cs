
public static class PlayerExtensions
{
    public static ZDO GetZDO(this Player player)
    {
        return player.m_nview.GetZDO();
    }
}
