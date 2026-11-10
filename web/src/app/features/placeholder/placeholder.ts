import { Component, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute } from '@angular/router';
import { map } from 'rxjs';

/** One "coming soon" page reused by every section until the real screens arrive (4d to 4f). */
@Component({
  selector: 'app-placeholder',
  templateUrl: './placeholder.html',
  styleUrl: './placeholder.scss',
})
export class Placeholder {
  protected readonly title = toSignal(
    inject(ActivatedRoute).data.pipe(
      map((data) =>
        typeof data['title'] === 'string' && data['title'] ? data['title'] : 'This section',
      ),
    ),
    { initialValue: 'This section' },
  );
}
