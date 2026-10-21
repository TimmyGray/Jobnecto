import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ConfirmDialogComponent } from './confirm-dialog';

/**
 * jsdom does not implement the modal parts of <dialog>, so stand in for just
 * enough of them: showModal/close toggle `open` and `close` fires as in a browser.
 */
function stubDialog(): () => void {
  const proto = HTMLDialogElement.prototype;
  const original = { showModal: proto.showModal, close: proto.close };
  proto.showModal = function (this: HTMLDialogElement) {
    this.setAttribute('open', '');
    // A browser moves focus into the dialog as it opens.
    this.querySelector('button')?.focus();
  };
  proto.close = function (this: HTMLDialogElement) {
    this.removeAttribute('open');
    // A browser takes focus out of a dialog that closes.
    if (this.contains(document.activeElement)) {
      (document.activeElement as HTMLElement).blur();
    }
    this.dispatchEvent(new Event('close'));
  };
  return () => {
    proto.showModal = original.showModal;
    proto.close = original.close;
  };
}

describe('ConfirmDialogComponent', () => {
  let fixture: ComponentFixture<ConfirmDialogComponent>;
  let restore: () => void;
  let confirms: number;
  let cancels: number;
  let opener: HTMLButtonElement;

  const dialog = (): HTMLDialogElement => fixture.nativeElement.querySelector('dialog');
  const buttons = (): HTMLButtonElement[] =>
    Array.from(fixture.nativeElement.querySelectorAll('dialog button'));
  const cancelButton = (): HTMLButtonElement => buttons()[0];
  const confirmButton = (): HTMLButtonElement => buttons()[1];
  const set = (name: string, value: unknown): void => {
    fixture.componentRef.setInput(name, value);
    fixture.detectChanges();
  };

  beforeEach(() => {
    restore = stubDialog();
    fixture = TestBed.createComponent(ConfirmDialogComponent);
    confirms = 0;
    cancels = 0;
    fixture.componentInstance.confirm.subscribe(() => confirms++);
    fixture.componentInstance.cancel.subscribe(() => cancels++);
    fixture.componentRef.setInput('headline', 'Delete this résumé?');
    fixture.componentRef.setInput('message', 'This cannot be undone from the app.');
    fixture.componentRef.setInput('confirmLabel', 'Delete résumé');
    opener = document.createElement('button');
    document.body.appendChild(opener);
    opener.focus();
    fixture.detectChanges();
  });

  afterEach(() => {
    opener.remove();
    restore();
  });

  it('is closed until opened', () => {
    expect(dialog().hasAttribute('open')).toBe(false);
  });

  it('opens and closes with the input', () => {
    set('open', true);
    expect(dialog().hasAttribute('open')).toBe(true);

    set('open', false);
    expect(dialog().hasAttribute('open')).toBe(false);
  });

  it('renders the headline and message and labels/describes the dialog with them', () => {
    set('open', true);

    const heading = dialog().querySelector('h2')!;
    const message = dialog().querySelector('p:not([role="status"])')!;
    expect(heading.textContent).toContain('Delete this résumé?');
    expect(message.textContent).toContain('cannot be undone');
    expect(dialog().getAttribute('aria-labelledby')).toBe(heading.id);
    expect(dialog().getAttribute('aria-describedby')).toBe(message.id);
  });

  it('omits the description when there is no message', () => {
    set('message', '');
    set('open', true);

    expect(dialog().querySelector('p:not([role="status"])')).toBeNull();
    expect(dialog().hasAttribute('aria-describedby')).toBe(false);
  });

  it('puts Cancel first and gives it initial focus, never the destructive button', () => {
    set('destructive', true);
    set('open', true);

    expect(cancelButton().textContent?.trim()).toBe('Cancel');
    expect(confirmButton().textContent?.trim()).toBe('Delete résumé');
    expect(document.activeElement).toBe(cancelButton());
  });

  it('uses custom labels', () => {
    set('cancelLabel', 'Keep it');
    set('confirmLabel', 'Remove');
    set('open', true);

    expect(cancelButton().textContent?.trim()).toBe('Keep it');
    expect(confirmButton().textContent?.trim()).toBe('Remove');
  });

  it('emits confirm exactly once when Confirm is clicked', () => {
    set('open', true);

    confirmButton().click();

    expect(confirms).toBe(1);
    expect(cancels).toBe(0);
  });

  it('emits cancel exactly once when Cancel is clicked', () => {
    set('open', true);

    cancelButton().click();

    expect(cancels).toBe(1);
    expect(confirms).toBe(0);
  });

  it('treats Escape (the native cancel event) as cancel and lets the host close it', () => {
    set('open', true);
    const event = new Event('cancel', { cancelable: true });

    dialog().dispatchEvent(event);

    expect(cancels).toBe(1);
    expect(event.defaultPrevented).toBe(true);
    expect(dialog().hasAttribute('open')).toBe(true);
  });

  /** A real backdrop click is a press and a release, both targeting the dialog element. */
  const backdropClick = (): void => {
    dialog().dispatchEvent(new MouseEvent('mousedown', { bubbles: true }));
    dialog().click();
  };

  it('treats a click on the backdrop (the dialog element itself) as cancel', () => {
    set('open', true);

    backdropClick();

    expect(cancels).toBe(1);
  });

  it('does not cancel when a press starts inside the content and is released on the backdrop', () => {
    set('open', true);

    dialog()
      .querySelector('p')!
      .dispatchEvent(new MouseEvent('mousedown', { bubbles: true }));
    dialog().click();

    expect(cancels).toBe(0);
  });

  it('does not cancel on a bare click that had no press on the backdrop', () => {
    set('open', true);

    dialog().click();

    expect(cancels).toBe(0);
  });

  it('forgets the press after the click, so a later stray click does not cancel', () => {
    set('open', true);
    backdropClick();
    expect(cancels).toBe(1);

    dialog().click();

    expect(cancels).toBe(1);
  });

  it('reports a native close it did not cause as a cancel', () => {
    set('open', true);

    dialog().dispatchEvent(new Event('close'));

    expect(cancels).toBe(1);
  });

  it('puts itself back if something closes it natively while busy', () => {
    set('busy', true);
    set('open', true);
    dialog().removeAttribute('open');

    dialog().dispatchEvent(new Event('close'));

    expect(dialog().hasAttribute('open')).toBe(true);
    expect(cancels).toBe(0);
  });

  it('does not report its own close as a cancel', () => {
    set('open', true);

    set('open', false);

    expect(cancels).toBe(0);
  });

  it('announces the busy message politely while busy and nothing otherwise', () => {
    set('busyMessage', 'Deleting résumé…');
    set('open', true);
    const live = (): string => dialog().querySelector('[role="status"]')!.textContent!.trim();
    expect(live()).toBe('');

    set('busy', true);
    expect(live()).toBe('Deleting résumé…');
    expect(dialog().querySelector('[role="status"]')!.getAttribute('aria-live')).toBe('polite');
  });

  it('returns focus to returnFocusTo when given, even if the opener never had focus', () => {
    const target = document.createElement('button');
    document.body.appendChild(target);
    document.body.focus();
    set('returnFocusTo', target);

    set('open', true);
    set('open', false);

    expect(document.activeElement).toBe(target);
    target.remove();
  });

  it('does not throw if the opener was removed while the dialog was open', () => {
    const gone = document.createElement('button');
    document.body.appendChild(gone);
    gone.focus();
    set('open', true);
    gone.remove();

    expect(() => set('open', false)).not.toThrow();
  });

  it('does not treat a click inside the dialog content as a backdrop click', () => {
    set('open', true);

    dialog().querySelector('h2')!.click();
    dialog().querySelector('p')!.click();

    expect(cancels).toBe(0);
  });

  describe('while busy', () => {
    beforeEach(() => {
      set('busy', true);
      set('open', true);
    });

    it('marks Confirm aria-disabled and busy but keeps it a focusable button', () => {
      expect(confirmButton().getAttribute('aria-disabled')).toBe('true');
      expect(confirmButton().getAttribute('aria-busy')).toBe('true');
      expect(confirmButton().hasAttribute('disabled')).toBe(false);
    });

    it('ignores Confirm clicks', () => {
      confirmButton().click();
      confirmButton().click();

      expect(confirms).toBe(0);
    });

    it('ignores Cancel, Escape and the backdrop', () => {
      cancelButton().click();
      const event = new Event('cancel', { cancelable: true });
      dialog().dispatchEvent(event);
      dialog().dispatchEvent(new MouseEvent('mousedown', { bubbles: true }));
      dialog().click();

      expect(cancels).toBe(0);
      expect(event.defaultPrevented).toBe(true);
    });
  });

  it('is not aria-disabled when idle', () => {
    set('open', true);

    expect(confirmButton().getAttribute('aria-disabled')).toBe('false');
  });

  it('gives the destructive variant the danger token and the default variant the primary one', () => {
    set('open', true);
    expect(confirmButton().className).toContain('bg-action-primary');
    expect(confirmButton().className).not.toContain('bg-status-danger');

    set('destructive', true);
    expect(confirmButton().className).toContain('bg-status-danger');
    expect(confirmButton().className).not.toContain('bg-action-primary');
  });

  it('returns focus to the element that opened it when it closes', () => {
    opener.focus();
    set('open', true);
    expect(document.activeElement).toBe(cancelButton());

    set('open', false);

    expect(document.activeElement).toBe(opener);
  });

  it('does not throw if it is closed while already closed', () => {
    expect(() => set('open', false)).not.toThrow();
  });
});
