using System;

public static class NameSegment
{
    public static string Tests => "Tests";

    public static bool IsTest(string nameSegment) => string.Equals(nameSegment, Tests, StringComparison.InvariantCultureIgnoreCase);

    public static class TestType
    {
        public static string Unit => "Unit";

        public static string Functional => "Functional";

        public static string Acceptance => "Acceptance";
    }
}
