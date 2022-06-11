public class DataSyncHub :Hub
{
    public async Task SyncRecord(string Table, string Action, string Id)
    {
        await Clients.Others.SendAsync("ReceiveSyncRecord",
              Table, Action, Id);
    }
}
