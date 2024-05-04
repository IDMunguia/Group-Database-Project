using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;

using Spectre.Console;

while (true)
{
    Breadcrumb.Draw();

    var (familyAction, family) = FamilyUI.SelectOne();

    if (familyAction == FamilyUI.SelectOption.ShowReport)
    {
        ShowAllFamilyReport();
        continue;
    }
    else if (familyAction == FamilyUI.SelectOption.InsertSampleFamilies)
    {
        InsertSampleFamilies(true);
        continue;
    }
    else if (familyAction == FamilyUI.SelectOption.CreateNewFamily)
    {
        family = FamilyUI.Create();
    }
    else if (familyAction == FamilyUI.SelectOption.AssignSecretSanta)
    {
        AssignSecretSanta();
        continue;
    }

    PresentFamily(family);
}

static void ShowAllFamilyReport()
{
    (string Name, string FileName)[] everyFamily = Database.ListExistingFamilies().ToArray();

    if (everyFamily.Length == 0)
    {
        Console.WriteLine("No families found.");
        return;
    }

    var tree = new Tree("[yellow]Family Report[/]");
    foreach (var (familyName, _) in everyFamily)
    {
        var familyNode = tree.AddNode(familyName);
        var family = new Database(familyName).Family;
        if (family != null)
        {
            foreach (var member in family.Members)
            {
                var assigneeName = member.GiveToName ?? "-";
                var giftIdea = member.GiveToGiftIdea ?? "-";

                familyNode.AddNode($"{member.Name} - Assigned to: {assigneeName}, Gift Idea: {giftIdea}");
            }
        }
    }

    AnsiConsole.Render(tree);
    Console.WriteLine("\r\nPress any key to continue...");
    Console.Read();
}

static void InsertSampleFamilies(bool clearFirst = false)
{
    if (clearFirst)
    {
        var families = Database.ListExistingFamilies();
        foreach (var (name, _) in families)
        {
            var family1 = new Database(name);
            family1.Delete();
        }
    }

    var family = new Database("Smith");
    family.Family!.Members.Add(new Member { Name = "John", Birthday = new DateOnly(1980, 1, 1) });
    family.Family!.Members.Add(new Member { Name = "Jane", Birthday = new DateOnly(1982, 2, 2) });
    family.Save();

    var secondFamily = new Database("Johnson");
    secondFamily.Family!.Members.Add(new Member { Name = "Michael", Birthday = new DateOnly(1975, 5, 5) });
    secondFamily.Family!.Members.Add(new Member { Name = "Emily", Birthday = new DateOnly(1988, 8, 8) });
    secondFamily.Save();

    var thirdFamily = new Database("Philips");
    thirdFamily.Family!.Members.Add(new Member { Name = "Will", Birthday = new DateOnly(1990, 10, 10) });
    thirdFamily.Family!.Members.Add(new Member { Name = "Phil", Birthday = new DateOnly(1995, 12, 12) });
    thirdFamily.Save();
}

static void PresentFamily(Database? family)
{
    Breadcrumb.Forward(family!.Family!.Name);

    while (true)
    {
        Breadcrumb.Draw(true);
        FamilyUI.Show(family);
        var familyOption = FamilyUI.Menu(family);

        if (familyOption == FamilyUI.MenuOption.Rename)
        {
            FamilyUI.Edit(family);
            family.Save();
            continue;
        }
        else if (familyOption == FamilyUI.MenuOption.OpenMember)
        {
            var member = MemberUI.SelectOne(family);
            PresentMember(family, member);
            continue;
        }
        else if (familyOption == FamilyUI.MenuOption.AddMember)
        {
            var member = MemberUI.Create(family);
            family.Family!.Members.Add(member);
            family.Save();
            continue;
        }
        else if (familyOption == FamilyUI.MenuOption.Delete)
        {
            FamilyUI.Delete(family);
        }

        Breadcrumb.Back();
        return;
    }
}

static void PresentMember(Database family, Member member)
{
    Breadcrumb.Forward(member.Name);

    while (true)
    {
        Breadcrumb.Draw(true);
        MemberUI.Show(member);

        var selection = MemberUI.Menu(member);
        if (selection == MemberUI.MenuOption.Edit)
        {
            MemberUI.Edit(family, member);
            family.Save();
            continue;
        }
        else if (selection == MemberUI.MenuOption.Delete)
        {
            MemberUI.Delete(family, member);
            family.Save();
        }

        Breadcrumb.Back();
        return;
    }
}

static void AssignSecretSanta()
{
     Breadcrumb.Draw(); // Draw breadcrumb initially

    AnsiConsole.WriteLine("Running Secret Santa assignment...");
    var families = Database.ListExistingFamilies().Select(x => new Database(x.Name));

    foreach (var family in families)
    {
        foreach (var member in family.Family!.Members)
        {
            if (TryFetchRandomMember(member.GetUniqueName(family.Family.Name), families, out var assignee))
            {
                member.GiveToName = assignee!.Value.Name;
                member.GiveToGiftIdea = assignee!.Value.GiftIdea;
                family.Save();

                AnsiConsole.WriteLine($"{family.Family.Name}/{member.Name} assigned to {member.GiveToName} with the gift idea: {member.GiveToGiftIdea}");
            }
        }
    }
    static bool TryFetchRandomMember(string uniqueName, IEnumerable<Database> families, out (string Name, string GiftIdea)? assignee)
    {
    assignee = families
        .SelectMany(x => x.Family!.Members.Select(y => new {UniqueName = y.GetUniqueName(x.Family.Name), Member = y}))
        .Where(x => x.UniqueName != uniqueName)
        .Where(x => string.IsNullOrEmpty(x.Member.GiveToName) || x.Member.GiveToName != "-")
        .Where(x => !x.Member.AvoidMembers.Contains(uniqueName))
        .OrderBy(x => Guid.NewGuid())
        .Select(x => (x.Member.Name, x.Member.GiftIdea))
        .FirstOrDefault();

    return assignee is not null;
    }

    Console.WriteLine("\r\nPress any key to continue...");
    Console.Read();
}