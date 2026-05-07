# Mock Cases and Demo Script
## Multi-Agent POC for User Story and Requirements Review

## 1. Purpose

This document defines the demo mock cases to demonstrate:

- dynamic agent selection;
- optional use of **Amazon Bedrock** or **Ollama**;
- conversation and decision logging;
- supervisor final consolidation;
- tension resolution between agents.

The idea is that the demo has increasing difficulty and clearly shows that **not all agents are executed every time**.

---

## 2. Suggested demo structure

For each case it is recommended to show:

1. input story;
2. chosen provider:
   - Bedrock or Ollama;
3. supervisor decision:
   - invoked agents,
   - skipped agents;
4. findings per agent;
5. final result;
6. summarized log.

---

## 3. Case 1 - Simple text change

### Story
**Title:** Change button text  
**Text:** As a user, I want the "Save" button to say "Confirm".

### Demo objective
Show a simple case where the supervisor avoids over-executing agents.

### Expected agents
**Invoked:**
- clarity
- ux

**Skipped:**
- qa
- technical
- compliance

### Expected rationale
This is a low-impact copy/UI change.

### Expected findings
**Clarity**
- confirm whether the change applies to one screen or all;
- validate whether "Confirm" is the correct business term.

**UX**
- review consistency with the rest of the interface;
- verify that the new text does not create ambiguity.

### Expected result
**Status:** green or low yellow.

### What to show from the log
- selected_agents with only clarity and ux;
- skipped_agents with reasons;
- brief final result.

---

## 4. Case 2 - Password reset

### Story
**Title:** Reset password  
**Text:** As a user, I want to be able to reset my password from the login screen to recover access to my account.

### Demo objective
Show a functional UI story where strong compliance or deep technical analysis is not needed.

### Expected agents
**Invoked:**
- clarity
- qa
- ux

**Skipped:**
- technical (optional)
- compliance

### Expected findings
**Clarity**
- it is not clarified what happens if the email does not exist;
- link expiration is not defined;
- it is not defined whether there is an attempt limit.

**QA**
- acceptance criteria are missing;
- invalid scenarios are missing;
- behavior for expired link is missing.

**UX**
- the message should be generic;
- confirmation feedback is needed;
- there should be a simple return to login.

### Expected result
**Status:** yellow.

### Supervisor consolidation example
- the story is useful but incomplete;
- add link expiration;
- define response for non-existent email;
- include Given/When/Then criteria.

### What to show from the log
- the supervisor skips compliance due to absence of explicit data/regulation;
- the reason for not invoking technical or invoking it in optional mode is visible.

---

## 5. Case 3 - Automatic notification retries

### Story
**Title:** Automatic retries  
**Text:** As a system, I need to automatically retry sending failed notifications up to 3 times before marking them as a definitive error.

### Demo objective
Show a backend story where UX does not participate.

### Expected agents
**Invoked:**
- clarity
- qa
- technical

**Skipped:**
- ux
- compliance

### Expected findings
**Clarity**
- the interval between retries is not defined;
- it is not clarified which errors are retryable;
- what constitutes a definitive error is not defined.

**QA**
- test 1st, 2nd, and 3rd retry;
- test transient vs permanent error;
- test that there are no duplicates.

**Technical**
- possible use of scheduler/queue;
- duplication risk;
- need for idempotency;
- need for metrics and monitoring.

### Expected result
**Status:** yellow.

### What to show from the log
- supervisor marks "backend story";
- UX skipped with explicit reason;
- technical provides differential value.

---

## 6. Case 4 - Personal data download

### Story
**Title:** Download personal report  
**Text:** As a customer, I want to download a report with my personal data and transactions from the last year.

### Demo objective
Show the activation of the compliance agent and a higher severity output.

### Expected agents
**Invoked:**
- clarity
- qa
- technical
- compliance

**Skipped:**
- ux (optional depending on implementation)

### Expected findings
**Clarity**
- file format is not defined;
- it is not defined whether generation is immediate or asynchronous;
- exact configurable range is not clarified.

**QA**
- validate authorization;
- test expected content;
- test ranges, volume, and errors.

**Technical**
- evaluate asynchronous generation;
- temporary storage;
- file expiration;
- performance with large volumes.

**Compliance**
- validate holder identity;
- do not expose data to third parties;
- record audit trail;
- define generated artifact expiration.

### Expected result
**Status:** red or high yellow, depending on the severity you want to demonstrate.

### What to show from the log
- supervisor detects sensitive data signals;
- compliance activated with explicit reason;
- possible conflict between UX speed and security/compliance.

---

## 7. Case 5 - Edit shipping address

### Story
**Title:** Edit shipping address  
**Text:** As a user, I want to edit my shipping address from my profile.

### Demo objective
Show conflict resolution between UX and technical agents.

### Expected agents
**Invoked:**
- clarity
- qa
- technical
- ux

**Skipped:**
- compliance (unless you want to harden the case)

### Expected findings
**Clarity**
- it is not clarified whether it applies to already generated orders;
- the editing deadline is not defined.

**QA**
- test non-dispatched orders;
- test orders in preparation;
- test invalid changes by country/postal code.

**Technical**
- changing the address may impact orders already in process;
- rules by order status need to be defined.

**UX**
- ideally editing should be simple and immediate;
- the user should receive clear feedback about restrictions.

### Expected conflict
- UX wants quick editing;
- technical requires restriction by order status.

### Expected supervisor resolution
- allow editing only for non-dispatched orders;
- show a visible restriction in the UI;
- for advanced orders, suggest contacting support.

### Expected result
**Status:** yellow.

### What to show from the log
- `conflict_detected` event;
- `supervisor_resolution` event;
- consolidated final result.

---

## 8. Suggested script for a 10-minute demo

## Block 1 - Introduction (1 minute)
Explain:
- there are several specialized agents;
- the supervisor decides who to consult;
- it can run with Bedrock or Ollama;
- a log of the entire execution is saved.

## Block 2 - Simple case (2 minutes)
Execute **Case 1**:
- show that only 2 agents are invoked;
- highlight that the system avoids unnecessary work.

## Block 3 - Functional case with UI (2 minutes)
Execute **Case 2**:
- show clarity + QA + UX;
- review findings;
- show selection log.

## Block 4 - Backend case (2 minutes)
Execute **Case 3**:
- show that UX is not used;
- show the technical agent's value.

## Block 5 - Sensitive or conflicting case (2 minutes)
Choose one:
- **Case 4** to activate compliance, or
- **Case 5** to show conflict between agents.

## Block 6 - Closing (1 minute)
Highlight:
- real specialization;
- dynamic selection;
- traceability;
- interchangeable providers;
- utility for refining stories.

---

## 9. Example summarized output to show

```json
{
  "executionId": "exec-demo-005",
  "provider": "bedrock",
  "model": "example-model",
  "status": "yellow",
  "invokedAgents": ["clarity", "qa", "technical", "ux"],
  "skippedAgents": [
    {
      "agent": "compliance",
      "reason": "No personal data or regulatory requirements detected"
    }
  ],
  "issues": [
    "The deadline for editing the address is not defined",
    "Behavior for already prepared orders is not clarified"
  ],
  "conflicts": [
    "UX proposes immediate editing; technical requires restriction by order status"
  ],
  "resolution": [
    "Allow editing only for non-dispatched orders",
    "Show restriction in UI",
    "Redirect to support if the order is already in preparation"
  ]
}
```

---

## 10. Practical recommendation

If you are going to show the system live:

- run **Case 1** with Ollama;
- run **Case 4 or 5** with Bedrock;

this way you also demonstrate that the architecture supports both providers without changing the functional flow.

---

## 11. Suggested mock files

In the `mock_inputs/` folder of this package, JSON examples are included for:

- `01_label_change.json`
- `02_reset_password.json`
- `03_notification_retries.json`
- `04_personal_data_download.json`
- `05_edit_shipping_address.json`

These files are used to run the demo in a repeatable way.
