SELECT "Id", "Name", "TemplateType", "IsFinalized", "FilePath", "SelectedTokens", "MeetingId"
FROM "Templates"
ORDER BY "CreatedAt" DESC
LIMIT 10;
