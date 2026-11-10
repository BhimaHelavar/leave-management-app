import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

/**
 * Screen S-12. The same safe message is shown for an unknown address and for a page the person
 * may not open, so it never reveals whether a page exists.
 */
@Component({
  selector: 'app-not-found',
  imports: [RouterLink],
  templateUrl: './not-found.html',
  styleUrl: './not-found.scss',
})
export class NotFound {}
