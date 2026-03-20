using System.Collections.ObjectModel;
using FsCheck;
using FsCheck.Xunit;
using ReasonableLivePlayer.Models;

namespace ReasonableLivePlayer.Tests;

/// <summary>
/// Feature: midi-driven-transport, Property 7: Playlist remove decreases count
/// Validates: Requirements 6.4
/// </summary>
public class PlaylistRemoveTests
{
    /// <summary>
    /// For any playlist of songs and for any song in the playlist, removing it
    /// should decrease the playlist count by 1 and the removed song should no
    /// longer be present.
    /// </summary>
    [Property(MaxTest = 100)]
    public Property Remove_Decreases_Count_And_Song_Is_Gone()
    {
        return Prop.ForAll(
            Arb.From(
                from count in Gen.Choose(1, 30)
                from removeIdx in Gen.Choose(0, count - 1)
                select (count, removeIdx)
            ),
            tuple =>
            {
                var (count, removeIdx) = tuple;
                var songs = new ObservableCollection<Song>();
                for (int i = 0; i < count; i++)
                    songs.Add(new Song { FilePath = $@"C:\Music\song{i}.reason" });

                var songToRemove = songs[removeIdx];
                var originalCount = songs.Count;

                songs.Remove(songToRemove);

                return songs.Count == originalCount - 1
                    && !songs.Contains(songToRemove);
            });
    }
}
