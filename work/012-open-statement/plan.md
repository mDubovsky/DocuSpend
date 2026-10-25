# Plan: Open a statement the user chooses

Spec: [spec.md](spec.md)

## Approach

No new dependency. Everything the spec needs is already in Avalonia 11.3.22 and the .NET base library. The opening logic (reading the file, the size limit, the messages, what is shown) lives in `DocuSpend.Presentation`, where it is tested against real files without the toolkit. `DocuSpend.Desktop` only adds the Windows open window and dropping, both of which hand a file path to the view model.

Verified on 25 October 2026, against the Avalonia 11.3.22 packages in use (their API documentation files, and the source at tag `11.3.22`):
- **The open window:** `IStorageProvider.OpenFilePickerAsync(FilePickerOpenOptions)`, with `FileTypeFilter` (a list of `FilePickerFileType` with `Patterns`) and `AllowMultiple`. `StorageProviderExtensions.TryGetLocalPath` turns the chosen file into a path.
- **Windows' recent files:** Avalonia's Windows open window always sets `FOS_DONTADDTORECENT` (`src/Windows/Avalonia.Win32/Win32StorageProvider.cs`, line 24 at `11.3.22`), so a statement opened through it is not added to Windows' recent files. Nothing extra is needed; the hand check confirms it.
- **Dropping:** `DragDrop.SetAllowDrop`, the `DragEnter`, `DragOver`, `DragLeave` and `Drop` events, `DataFormats.Files`, and `DataObjectExtensions.GetFiles(IDataObject)`, which gives storage items: a folder is an `IStorageFolder`.
- **Dropping in tests:** `Avalonia.Headless` 11.3.22 has `HeadlessWindowExtensions.DragDrop(TopLevel, Point, RawDragEventType, IDataObject, DragDropEffects, RawInputModifiers)`, so the window tests can drop files and text onto the real window.
- **A statement that doesn't add up, or that has a part that can't be read:** the sample generator's test-only `RenderOptions` already produce both (`tests/DocuSpend.Statements.Tests/ReconciliationTests.cs`). The presentation tests use them the same way, in memory, so no new sample is committed.

### Opening, in `DocuSpend.Presentation`
- **`StatementFile.ReadAsync(path)`** reads a file into memory and answers either its bytes or a problem:
  - the file is missing, or can't be opened (in use with no sharing, no permission, a path error): `CannotOpen`;
  - the file is over 50 MB (`50 × 1024 × 1024` bytes), from its length, before reading: `TooLarge`;
  - otherwise all its bytes, read with `FileShare.ReadWrite | FileShare.Delete`, so a statement another program has open for reading (a PDF viewer) still opens.

  It reads on a background thread (`Task.Run`), so the window stays responsive. It writes nothing.
- **`OpenProblem`**: `Empty`, `NotAPdf`, `NotAVubStatement`, `Unreadable`, `DoesNotReconcile`, `CannotOpen`, `TooLarge`. Each `RejectionKind` maps to one. A test fails if a new `RejectionKind` has no mapping.
- **`Texts`** gains the start screen's texts, the button labels, "Výpis sa číta…", "Pustite výpis sem", the example label, the message start and end, one message per `OpenProblem`, and the two drop messages, all exactly as in the spec. The reader's own message is never used on screen.
- **`MainViewModel`** loses the sample list and gains:
  - `HasStatement`, `IsExample`, `IsReading`, `IsDraggingOver`, and `OpenMessage` (the message, or null);
  - `OpenFileAsync(path)`: reads, reads the statement, and on success shows it (`StatementViewModel.Show` already clears the selection) and clears the message. On failure, it keeps what is shown and sets the message: "Výpis sa neotvoril." + the problem's message, + "Zobrazený zostáva predchádzajúci výpis." when a statement is shown. It does nothing while `IsReading`;
  - `OpenCommand`: asks an `IStatementPicker` (an interface with one method, `Task<string?> PickAsync()`) for a path, and opens it; a null path (cancelled) changes nothing. Disabled while reading;
  - `OpenExampleCommand`: opens the example and sets `IsExample`. Available only when the example file exists;
  - `Drop(DroppedContent)`: `DroppedContent` is a small record of what was dropped (the file paths, and whether it held a folder or anything other than files). Several files, a folder or no files gives the drop message; one file opens it;
  - `CloseMessageCommand`.
- **The example**: the only file left of `SampleCatalog`. It is `01-typical-month.pdf`, in the `samples` folder next to the program, as today; the other four valid samples no longer ship. `SampleCatalog` and `SampleItem` are removed.

### The window, in `DocuSpend.Desktop`
- **`StatementPicker`** implements `IStatementPicker` with the window's `StorageProvider`: one file, the filters "Výpisy vo formáte PDF" (`*.pdf`) and "Všetky súbory" (`*.*`), in that order. A chosen item without a local path gives `CannotOpen` (this doesn't happen on Windows, but the case is handled).
- **The start screen** is a card shown when `HasStatement` is false. The statement's cards, the rule form and the rule list are shown when it is true. Both are in `MainWindow.axaml`, using only the look's resources (the existing check enforces this).
- **The header**: the sample choice box is replaced by the "Otvoriť výpis…" button (a normal button), the period, and the example label when `IsExample` is true. Under it is the message, in the existing `message error` style, with a "Zavrieť" button.
- **Ctrl+O**: a `KeyBinding` on the window to `OpenCommand`.
- **Dropping**: `AllowDrop` on the window. The window handles `DragEnter`/`DragOver` (sets `IsDraggingOver` when files are dragged, and allows a copy for anything, so a wrong drop still gets its message), `DragLeave` and `Drop`. Drag events in Avalonia only bubble, so the handlers are registered for bubbling with handled events too: the window sees a drop even when the rule form's text box handled it. The drop builds a `DroppedContent` from the drag's `DataTransfer` items (Avalonia 11.3's replacement for the obsolete `IDataObject`, which the build refuses as a warning) and hands it to `MainViewModel.DropAsync`. While dragging, an overlay over the window says "Pustite výpis sem", in a new style in `Look.axaml`.
- **The project file** ships only `01-typical-month.pdf`.

### Nothing kept
- The only code that writes files is `SettingsStore` and `SpikeRuleStore`, and neither changes. A new test in `DocuSpend.Presentation.Tests` reads the source files under `src/` and fails if any file other than those two uses a file-writing API (`File.Write…`, `File.Create`, `File.Copy`, `File.Move`, `File.Append…`, `FileMode.Create`/`OpenOrCreate`/`Append`, `FileAccess.Write`, `StreamWriter`, `Directory.CreateDirectory`). It can't prove that nothing is written, but it makes a new place that writes something a deliberate change.
- A test opens every sample with the settings and rules in a temporary folder, changes the look and adds a rule, and then checks that nothing in that folder contains the samples' file names, their folder, or any account number, holder name or description from their records.
- `PRIVACY.md` gets a paragraph on what happens to an opened file and on Windows' folder memory, and its "Where things stand" note is updated.

### Screenshots and existing tests
- `tools/DocuSpend.Screenshots` opens files with `OpenFileAsync` from `samples/public` in the repository, since only the example ships. Its states become: the start screen; the start screen with the message for `06`; the example (`01`) with a category selected; the busy month (`02`); the empty month (`04`); `02` shown with the message for `07` above it; the rule form with an error, and with a confirmation. The dragging overlay is captured too, by setting `IsDraggingOver`.
- The window tests and the presentation tests that used `SampleCatalog` open files the same way.

### Alternatives considered
- **Reading through Avalonia's `IStorageFile.OpenReadAsync`** instead of a path: it would put the reading in the desktop project, outside the presentation tests, and the size and locking cases would need the toolkit to test. On Windows every chosen or dropped file has a local path.
- **The open window in the window's code only, without `IStatementPicker`**: simpler, but then "cancelling changes nothing" and "disabled while reading" couldn't be tested without a real dialog.
- **Keeping all five valid samples beside the program**: the intent decided on one example.
- **A new sample for "doesn't add up"**: the generator's test options already make one in memory, and a committed sample would be one more file to keep in sync.
- **Refusing by file extension before reading**: the intent decided files are judged by their content.

### Layout
```
src/DocuSpend.Presentation/   StatementFile, OpenProblem, IStatementPicker, DroppedContent; MainViewModel, Texts changed; SampleCatalog removed
src/DocuSpend.Desktop/        StatementPicker; MainWindow.axaml(.cs), Look/Look.axaml, App.axaml.cs, the project file changed
tools/DocuSpend.Screenshots/  the new states
tests/…                       new and changed tests in the presentation and desktop tests
```

## Prerequisites
- Items 002, 003, 005 and 011 merged. ✔
- The maintainer reviews the screenshots, and does the hand checks with the built application and their own statements.

## Steps
Test-first for everything in `DocuSpend.Presentation`. One commit per step, each after the maintainer's approval. The first commit sets the intent's status to `in-progress`.

1. **Reading a file**: `StatementFile` and `OpenProblem`, with tests for a missing file, a locked file, a file open in another program, a file over 50 MB (a sparse temporary file), an empty file, and a sample.
2. **The messages**: the new `Texts`, the mapping from `RejectionKind` to `OpenProblem`, and the tests that every problem has a message, none is in English, and none contains anything from the samples.
3. **Opening in the view model**: `MainViewModel` without the sample list; `OpenFileAsync`, `OpenCommand` with a test picker, `OpenExampleCommand`, `Drop`, `CloseMessageCommand`, `IsReading`. Tests for every sample, both kinds of statement built with the generator, a failure with and without a statement shown, the message being replaced and closed, the drop cases, and rules applying to an opened statement. `SampleCatalog` is removed, and its users (the window, the screenshot tool, the tests) open files instead, so the build stays green.
4. **The window**: the start screen, the header, the message, Ctrl+O, `StatementPicker`, dropping with the overlay, and only the example in the project file. Window tests: the start screen, the example, a file dropped and text dropped through the headless platform, and a drop onto the rule form's text box. Screenshots of the new states in both looks (**stop: screenshot review**).
5. **Nothing kept**: the source check on file writing, the temporary-folder check, and the `PRIVACY.md` changes (**stop: the maintainer reads the PRIVACY.md wording**).
6. **Hand checks and documentation** (**stop: hand checks**): the maintainer checks the open window, Ctrl+O, dropping from Explorer, Windows' recent files and their own statements, with the CI build. `AGENTS.md` and `README.md` stop saying the application shows the synthetic samples, and anything that surprised during the build is noted in this plan. The intent's status becomes `done`.

## Risks and unknowns
- **The text box takes the drop.** Avalonia's text box could handle a file drop before the window sees it. Window handlers that see handled events too still get it; the window test drops a file onto the text box. (Found in the build: drag events only bubble, so tunnelling handlers, as first planned, never fire.)
- **The headless drop.** If the headless `DragDrop` helper doesn't deliver files the way Windows does, the drop logic is still tested in `MainViewModel.Drop`, and dropping from Explorer stays a hand check, which it is anyway.
- **A locked file in tests.** A file held with `FileShare.None` by the test itself gives the same error as one held by another program, on Windows and Linux. If Linux behaves differently (its locks are advisory), that test runs on Windows only and says why.
- **Reading on a background thread.** The view model's properties change after an `await`, back on the UI thread, because the command runs there. The tests await the method directly. A file read on a network drive can take long; the window stays usable and shows "Výpis sa číta…".
- **Windows' folder memory** may also keep the last file name in the open window's suggestions on some Windows versions. If the hand check finds that, it goes into `PRIVACY.md` with the folder memory, since it is the same Windows setting.

## Verification

| Acceptance criterion | Checked by |
|---|---|
| Starts on the start screen, with its texts and buttons, without statement, charts or rule form | Window test; view model test (`HasStatement` false at start) |
| "Vyskúšať na ukážke" shows `01` with the example label | View model test; window test |
| Each valid sample opened from disk shows the same as the reader and categorizer give, without the label | View model test over `01`–`05`, against `StatementViewModel` for the same statement |
| Each invalid sample gives its message, and what was shown stays and works | View model test over `06`–`09`, from the start screen and with a statement shown |
| Doesn't add up / part can't be read give their own messages | View model tests with statements rendered by the generator's test options |
| A deleted or locked file gives "can't be opened" | `StatementFile` and view model tests |
| Over 50 MB gives "too large" without reading | `StatementFile` test with a sparse temporary file; the limit is checked from the file's length, before any byte is read |
| A new statement removes the message; closing it keeps the statement | View model tests |
| A drop opens like the open window; several files, a folder or text give their message | View model tests for `Drop`; window tests through the headless platform |
| Saved rules apply to an opened statement; adding a rule works | View model test |
| Every message is Slovak and contains nothing from the file | Texts tests against the samples' records and file names |
| Nothing DocuSpend wrote mentions the statements | Temporary-folder test; source check on file writing |
| The open window: filters, opening, cancelling, Ctrl+O | Hand check (maintainer); cancelling and the command also by view model tests with a test picker |
| Dropping from Explorer, with "Pustite výpis sem" | Hand check (maintainer); window test for the overlay |
| Not in Windows' recent files | Hand check (maintainer); `FOS_DONTADDTORECENT` verified in Avalonia's source |
| The maintainer's statements open in the built application | Hand check (maintainer), CI build |
| Screenshots approved | Review at step 4 and again at step 6 |
| PRIVACY.md updated | Review at step 5 |
| Privacy checks pass, no new entries | `dotnet test` in CI; no change to `privacy/` |
