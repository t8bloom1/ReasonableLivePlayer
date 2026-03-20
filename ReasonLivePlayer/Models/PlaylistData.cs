namespace ReasonLivePlayer.Models;

public record PlaylistData(
    int Version,
    List<string> Songs
);
