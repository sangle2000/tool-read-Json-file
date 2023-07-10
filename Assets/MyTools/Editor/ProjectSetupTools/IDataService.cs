public interface IDataService
{
    bool SaveData<T>(string RelativePath, T Data, bool Encrypted, string KEY, string IV);

    T LoadData<T>(string RelativePath, bool Encrypted, string KEY, string IV);
}
