using GamingCommander.Core.Models;

namespace GamingCommander.Core;

public interface IConfigService
{
    AppConfig Load();
    void Save(AppConfig config);
}
