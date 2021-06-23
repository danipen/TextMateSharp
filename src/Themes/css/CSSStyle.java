/**
 *  Copyright (c) 2015-2017 Angelo ZERR.
 * This program and the accompanying materials are made
 * available under the terms of the Eclipse Public License 2.0
 * which is available at https://www.eclipse.org/legal/epl-2.0/
 *
 * SPDX-License-Identifier: EPL-2.0
 *
 *  Contributors:
 *  Angelo Zerr <angelo.zerr@gmail.com> - initial API and implementation
 */
package org.eclipse.tm4e.core.theme.css;

import org.eclipse.tm4e.core.theme.IStyle;
import org.eclipse.tm4e.core.theme.RGB;
import org.w3c.css.sac.SelectorList;

public class CSSStyle implements IStyle {

	private final SelectorList selector;
	private RGB color;
	private RGB backgroundColor;
	
	private boolean bold;
	private boolean italic;
	private boolean underline;
	private boolean strikeThrough;

	public CSSStyle(SelectorList selector) {
		this.selector = selector;
	}

	public void setColor(RGB color) {
		this.color = color;
	}

	@Override
	public RGB getColor() {
		return color;
	}

	@Override
	public RGB getBackgroundColor() {
		return backgroundColor;
	}
	
	public void setBackgroundColor(RGB backgroundColor) {
		this.backgroundColor = backgroundColor;		
	}
	
	public SelectorList getSelectorList() {
		return selector;
	}

	public void setBold(boolean bold) {
		this.bold = bold;
	}

	@Override
	public boolean isBold() {
		return bold;
	}

	public void setItalic(boolean italic) {
		this.italic = italic;
	}

	@Override
	public boolean isItalic() {
		return italic;
	}

	@Override
	public boolean isUnderline() {
		return underline;
	}

	public void setUnderline(boolean underline) {
		this.underline = underline;
	}

	@Override
	public boolean isStrikeThrough() {
		return strikeThrough;
	}
	
	public void setStrikeThrough(boolean strikeThrough) {
		this.strikeThrough = strikeThrough;
	}

}
