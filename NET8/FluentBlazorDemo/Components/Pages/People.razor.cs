﻿using Microsoft.AspNetCore.Components;

namespace FluentBlazorDemo.Components.Pages;

public partial class People : ComponentBase
{
    record Person(int PersonId, string Name, DateOnly BirthDate);

    IQueryable<Person> people = new[]
    {
        new Person(10895, "Jean Martin", new DateOnly(1985, 3, 16)),
        new Person(10944, "António Langa", new DateOnly(1991, 12, 1)),
        new Person(11203, "Julie Smith", new DateOnly(1958, 10, 10)),
        new Person(11205, "Nur Sari", new DateOnly(1922, 4, 27)),
        new Person(11898, "Jose Hernandez", new DateOnly(2011, 5, 3)),
        new Person(12130, "Kenji Sato", new DateOnly(2004, 1, 9)),
    }.AsQueryable();
    
    private void BtnClick()
    {
        Logger.LogInformation("Click me");
    }
}
