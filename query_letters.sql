SELECT "Id", "MeetingId", "ShareholderName", "TemplateId", "Status"
FROM "invitation_letters"
ORDER BY "CreatedAtUtc" DESC
LIMIT 5;
