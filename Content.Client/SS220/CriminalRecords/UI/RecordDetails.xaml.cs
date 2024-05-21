// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Client.Message;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Roles;
using Content.Shared.StationRecords;
using Content.Shared.StatusIcon;
using Robust.Client.AutoGenerated;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Client.SS220.CriminalRecords.UI;

[GenerateTypedNameReferences]
public sealed partial class RecordDetails : Control
{
    private readonly IPrototypeManager _prototype;
    private readonly IEntitySystemManager _sysMan;
    private readonly SpriteSystem _sprite;

    public RecordDetails()
    {
        RobustXamlLoader.Load(this);

        _prototype = IoCManager.Resolve<IPrototypeManager>();
        _sysMan = IoCManager.Resolve<IEntitySystemManager>();
        _sprite = _sysMan.GetEntitySystem<SpriteSystem>();
    }

    private string CapitalizeFirstLetter(string input)
    {
        if (input.Length == 0)
            return input;
        else if (input.Length == 1)
            return char.ToUpper(input[0]).ToString();
        else
            return ConcatNoReadOnlySpan(char.ToUpper(input[0]).ToString(), input.Substring(1));
    }
	
	// fixes roslyn being too smart and trolling the sandbox
	// probably will be fixed later and will need to be cleaned up
	private string ConcatNoReadOnlySpan(string a, string b)
	{
		return a + b;
	}

    public void LoadRecordDetails(GeneralStationRecord record, bool loadSecurity = true)
    {
        // Setup job field
        string jobTitle = record.JobTitle;
        string? jobColor = null;
        if (!string.IsNullOrEmpty(record.JobPrototype))
        {
            var jobPrototype = _prototype.Index<JobPrototype>(record.JobPrototype);
            jobTitle = jobPrototype.LocalizedName;
            jobColor = GetJobColor(record.JobPrototype);

            var iconPrototype = _prototype.Index<StatusIconPrototype>(jobPrototype.Icon);
            JobIcon.Texture = _sprite.Frame0(iconPrototype.Icon);
        }

        if (string.IsNullOrEmpty(jobColor))
            jobColor = Color.White.ToHexNoAlpha();

        var finalJobTitle = string.IsNullOrEmpty(jobTitle)
        ? Loc.GetString("criminal-records-ui-unknown-job")
        : CapitalizeFirstLetter(jobTitle);
        JobName.SetMarkup($"[color={jobColor}]{finalJobTitle}[/color]");

        // Setup gender, age and race fields (single label)
        var genderString = record.Gender switch
        {
            Gender.Female => Loc.GetString("identity-gender-feminine"),
            Gender.Male => Loc.GetString("identity-gender-masculine"),
            Gender.Epicene or Gender.Neuter or _ => Loc.GetString("identity-gender-person")
        };

        string species;
        if (string.IsNullOrEmpty(record.Species))
            species = Loc.GetString("criminal-records-ui-unknown");
        else
            species = Loc.GetString(record.Species);

        if (record.Profile != null)
        {
            var speciesProto = _prototype.Index<SpeciesPrototype>(record.Profile.Species);
            species = Loc.GetString(speciesProto.Name);
        }

        DetailsLabel.SetMarkup($"Возраст: {record.Age}   Раса: {species}   Пол: {genderString}");

        // DNA and fingerprint fields
        DnaLabel.Text = $"ДНК: {(string.IsNullOrEmpty(record.DNA) ? Loc.GetString("criminal-records-ui-unknown") : record.DNA)}";
        FingerprintsLabel.Text = $"Отпечатки: {(string.IsNullOrEmpty(record.Fingerprint) ? Loc.GetString("criminal-records-ui-unknown") : record.Fingerprint)}";

        // Characher photo in job uniform
        if (record.Profile != null && !string.IsNullOrEmpty(record.JobPrototype))
            CharVis.SetupCharacterSpriteView(record.Profile, record.JobPrototype);
        else
            CharVis.ResetCharacterSpriteView();

        // Criminal status over photo
        if (loadSecurity && record.CriminalRecords?.GetLastRecord() is { } criminalRecord)
        {
            CriminalStatusContainer.Visible = criminalRecord.RecordType.HasValue;
            if (criminalRecord.RecordType.HasValue)
            {
                StatusLabel.Visible = true;
                var recordType = _prototype.Index(criminalRecord.RecordType.Value);
                StatusLabel.SetMarkup($"[color={recordType.Color.ToHex()}][bold]{recordType.Name}[/bold][/color]");

                CriminalStatusIcon.Visible = recordType.StatusIcon.HasValue;
                if (recordType.StatusIcon.HasValue)
                {
                    var iconProto = _prototype.Index<StatusIconPrototype>(recordType.StatusIcon);
                    CriminalStatusIcon.Texture = _sprite.Frame0(iconProto.Icon);
                }
            }
        }
        else
        {
            CriminalStatusContainer.Visible = false;
        }
    }

    const float ADDITIONAL_COLOR_CHANNEL_VALUE = 0.25f;

    private string GetJobColor(string jobPrototypeId)
    {
        var departments = _prototype.EnumeratePrototypes<DepartmentPrototype>().ToList();
        departments.Sort((a, b) => a.Sort.CompareTo(b.Sort));

        foreach (var department in from department in departments
            from jobId in department.Roles
            where jobId == jobPrototypeId
            select department)
        {
            // make brighter cuz pure red/blue are too dark to be readable
            var color = department.Color;
            color = color.WithRed(MathF.Min(color.R + ADDITIONAL_COLOR_CHANNEL_VALUE, 1));
            color = color.WithGreen(MathF.Min(color.G + ADDITIONAL_COLOR_CHANNEL_VALUE, 1));
            color = color.WithBlue(MathF.Min(color.B + ADDITIONAL_COLOR_CHANNEL_VALUE, 1));
            return color.ToHex();
        }

        return Color.White.ToHex();
    }
}
