namespace ReasonableLivePlayer.Models;

public record PlaylistData(
    int Version,
    List<string> Songs
);
