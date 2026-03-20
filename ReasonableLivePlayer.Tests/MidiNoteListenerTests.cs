using FsCheck;
using FsCheck.Xunit;
using ReasonableLivePlayer.Automation;

namespace ReasonableLivePlayer.Tests;

/// <summary>
/// Feature: midi-driven-transport, Property 2: MIDI note filtering
/// Validates: Requirements 2.1
/// </summary>
public class MidiNoteListenerTests
{
    /// <summary>
    /// For any configured channel (0-15) and note number (0-127), and for any
    /// incoming MIDI message, IsMatchingNoteOn should return true if and only if
    /// the message is a note-on (0x90) with matching channel, matching note,
    /// and non-zero velocity.
    /// </summary>
    [Property(MaxTest = 100)]
    public Property NoteOn_Matches_Only_When_Channel_Note_Velocity_All_Match()
    {
        return Prop.ForAll(
            Arb.From(
                from configChannel in Gen.Choose(0, 15)
                from configNote in Gen.Choose(0, 127)
                from msgChannel in Gen.Choose(0, 15)
                from msgNote in Gen.Choose(0, 127)
                from velocity in Gen.Choose(0, 127)
                from messageType in Gen.Elements(0x80, 0x90, 0xA0, 0xB0, 0xC0, 0xD0, 0xE0)
                let status = messageType | msgChannel
                let midiData = status | (msgNote << 8) | (velocity << 16)
                select (configChannel, configNote, midiData, messageType, msgChannel, msgNote, velocity)
            ),
            tuple =>
            {
                var (configChannel, configNote, midiData, messageType, msgChannel, msgNote, velocity) = tuple;
                bool result = MidiNoteListener.IsMatchingNoteOn(midiData, configChannel, configNote);
                bool expected = messageType == 0x90
                    && msgChannel == configChannel
                    && msgNote == configNote
                    && velocity > 0;
                return result == expected;
            });
    }
}
