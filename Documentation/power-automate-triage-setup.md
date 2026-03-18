# Power Automate Setup: Triage Email Flows

This guide covers **two** Power Automate flows needed for the WPF issue triage system:

1. **Flow 1: Send Triage Email** — Receives a webhook from GitHub Actions and sends the triage report email
2. **Flow 2: Handle Email Reply** — Watches for replies and triggers the apply-triage workflow

## Prerequisites

- Access to [Power Automate](https://make.powerautomate.com/)
- An Office 365 account that can send/receive email (e.g., your `@microsoft.com` account)
- A GitHub Personal Access Token (PAT) with **`actions:write`** and **`repo`** scopes on `dotnet/wpf` (for Flow 2 only)

---

## Flow 1: Send Triage Email (Webhook → Email)

This flow receives the triage report from GitHub Actions and sends it as an email.

### Step 1: Create the Flow

1. Go to [Power Automate](https://make.powerautomate.com/)
2. Click **+ Create** → **Instant cloud flow**
3. Name it: `WPF Triage - Send Report Email`
4. Select trigger: **"When an HTTP request is received"**

### Step 2: Configure the HTTP Trigger

1. In the trigger, paste this **JSON Schema**:

```json
{
  "type": "object",
  "properties": {
    "subject": { "type": "string" },
    "body": { "type": "string" },
    "triageJson": { "type": "string" },
    "runId": { "type": "string" },
    "repoUrl": { "type": "string" }
  },
  "required": ["subject", "body"]
}
```

2. Set **Who can trigger the flow** to: "Anyone"
3. After saving, copy the **HTTP POST URL** — you'll need this for the GitHub secret

### Step 3: Add "Send an Email (V2)" Action

1. Add action: **"Send an email (V2)"** (Office 365 Outlook)
2. Configure:
   - **To**: `waveteam@microsoft.com`
   - **Subject**: Select `subject` from the dynamic content (from the trigger)
   - **Body**: Select `body` from the dynamic content
   - **Importance**: Normal
   - **Is HTML**: Yes (the body contains markdown-converted HTML)

### Step 4: Save and Copy the Webhook URL

1. **Save** the flow
2. Go back to the HTTP trigger step
3. Copy the **HTTP POST URL** — it looks like:
   ```
   https://prod-XX.westus.logic.azure.com:443/workflows/abc123.../triggers/manual/paths/invoke?api-version=...&sp=...&sv=...&sig=...
   ```

### Step 5: Add the Webhook URL as a GitHub Secret

1. Go to **https://github.com/dotnet/wpf/settings/secrets/actions**
2. Click **"New repository secret"**
   - **Name**: `TRIAGE_WEBHOOK_URL`
   - **Value**: *(paste the Power Automate HTTP POST URL)*

> **That's it!** This is the only GitHub secret you need. No SMTP credentials required.

### Step 6: Test

1. Go to GitHub → **Actions** → **"Weekly Issue Triage"** → **"Run workflow"**
2. The workflow will generate the report and POST to Power Automate
3. Power Automate sends the email to waveteam@microsoft.com
4. Check your inbox!

---

## Flow 2: Handle Email Reply (Reply → Apply Labels)

```
┌──────────────────────────────┐
│  Trigger: New email arrives  │
│  (reply to triage report)    │
└──────────────┬───────────────┘
               │
               ▼
┌──────────────────────────────┐
│  Condition: Subject contains │
│  "[WPF Triage]" AND "RE:"   │
└──────────────┬───────────────┘
               │ Yes
               ▼
┌──────────────────────────────┐
│  Compose: Extract JSON block │
│  from email body             │
└──────────────┬───────────────┘
               │
               ▼
┌──────────────────────────────┐
│  HTTP: POST to GitHub API    │
│  Trigger apply-triage.yml    │
└──────────────────────────────┘
```

## Step-by-Step Setup (Flow 2)

### Step 1: Create a New Flow

1. Go to [Power Automate](https://make.powerautomate.com/)
2. Click **+ Create** → **Automated cloud flow**
3. Name it: `WPF Triage - Email Reply Handler`
4. Skip the trigger selection — we'll add it manually

### Step 2: Add the Email Trigger

1. Search for **"When a new email arrives (V3)"** (Office 365 Outlook)
2. Configure:
   - **Folder**: Inbox (or the shared mailbox folder)
   - **Subject Filter**: `[WPF Triage]`
   - **Include Attachments**: No
   - **Importance**: Any
3. Click **Show advanced options**:
   - **To**: `waveteam@microsoft.com` (optional, for specificity)

### Step 3: Add a Condition

1. Add a **Condition** action
2. Set the condition:
   - **Subject** → **contains** → `RE:` (or `Re:`)
3. This ensures we only process replies, not the original triage email

### Step 4: Extract JSON from the Email Body (Yes branch)

1. In the **Yes** branch, add a **Compose** action
2. Name it: `Extract JSON`
3. Set the **Inputs** to this expression:

```
substring(
  triggerOutputs()?['body/body'],
  add(indexOf(triggerOutputs()?['body/body'], '```json'), 7),
  sub(
    indexOf(triggerOutputs()?['body/body'], '```', add(indexOf(triggerOutputs()?['body/body'], '```json'), 7)),
    add(indexOf(triggerOutputs()?['body/body'], '```json'), 7)
  )
)
```

> **What this does**: Finds the ```` ```json ```` block in the email reply and extracts
> the JSON content between the markers. This works because the original triage email
> contains the editable JSON in a fenced code block.

**Alternative (simpler but less robust)**: If the expression above doesn't work
with your email client's HTML formatting, use this approach instead:

1. Add a **Compose** action with the expression: `triggerOutputs()?['body/body']`
2. Add an **Office Script** or **Azure Function** to parse the JSON from HTML

### Step 5: Trigger the GitHub Actions Workflow

1. Add an **HTTP** action
2. Configure:
   - **Method**: `POST`
   - **URI**: `https://api.github.com/repos/dotnet/wpf/actions/workflows/apply-triage.yml/dispatches`
   - **Headers**:
     | Key | Value |
     |-----|-------|
     | `Authorization` | `Bearer <YOUR_GITHUB_PAT>` |
     | `Accept` | `application/vnd.github.v3+json` |
     | `X-GitHub-Api-Version` | `2022-11-28` |
   - **Body**:
     ```json
     {
       "ref": "main",
       "inputs": {
         "triage_json": @{outputs('Extract_JSON')},
         "dry_run": "false"
       }
     }
     ```

> **Security note**: Store the GitHub PAT as a Power Automate **secret** or
> **environment variable** rather than hardcoding it in the flow.

### Step 6: Add Error Handling (Optional)

1. After the HTTP action, add a **Condition** to check the response status
2. If status code is not `204`:
   - Send a failure notification email to the person who replied
3. If status code is `204`:
   - Optionally send a confirmation: "Triage labels are being applied!"

### Step 7: Save and Test

1. **Save** the flow
2. **Test** by sending a test reply to a triage email with a modified JSON block
3. Verify the `apply-triage.yml` workflow runs on GitHub Actions

## Storing the GitHub PAT Securely

Instead of hardcoding the PAT in the HTTP action:

1. Go to **Power Automate** → **Solutions** → create or use an existing solution
2. Add an **Environment Variable** of type **Secret**:
   - Name: `GitHubPAT`
   - Value: your GitHub PAT
3. Reference it in the HTTP action as: `@{parameters('GitHubPAT')}`

Alternatively, use **Azure Key Vault**:
1. Store the PAT in Azure Key Vault
2. Use the **Azure Key Vault** connector in Power Automate
3. Add a "Get secret" action before the HTTP action

## Troubleshooting

### JSON extraction fails
- Different email clients format the reply body differently (HTML entities, line breaks, etc.)
- Check the raw email body in the flow run history
- You may need to adjust the expression to handle `&quot;`, `<br>`, or `&#10;` characters
- Consider using a simple regex-based Azure Function as an alternative

### GitHub API returns 404
- Verify the PAT has `actions:write` scope
- Verify the workflow file `apply-triage.yml` exists on the `main` branch
- Check the repository name is correct: `dotnet/wpf`

### GitHub API returns 422
- The `triage_json` input may exceed the maximum size for workflow dispatch inputs
- The JSON may contain characters that need escaping
- Try with a smaller subset of issues first

### Flow doesn't trigger
- Ensure the email subject exactly contains `[WPF Triage]`
- Check the shared mailbox permissions
- Verify the trigger is monitoring the correct folder

## Manual Fallback

If the Power Automate flow isn't working, you can always trigger the apply workflow manually:

1. Go to https://github.com/dotnet/wpf/actions/workflows/apply-triage.yml
2. Click **"Run workflow"**
3. Paste the edited JSON into the `triage_json` input
4. Set `dry_run` to `true` first to preview, then `false` to apply
