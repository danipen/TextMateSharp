#include "onigwrap.h"

regex_t *onigwrap_create(char *pattern, int len, int ignoreCase, int multiline)
{
	regex_t *reg;

	OnigErrorInfo einfo;

	OnigOptionType onigOptions = ONIG_OPTION_NONE | ONIG_OPTION_CAPTURE_GROUP;

	if (ignoreCase == 1)
		onigOptions |= ONIG_OPTION_IGNORECASE;

	if (multiline == 1)
		onigOptions |= ONIG_OPTION_MULTILINE;

	OnigUChar *stringStart = (OnigUChar*) pattern;
	OnigUChar *stringEnd   = (OnigUChar*) pattern + len;
	
    int res = onig_new(
        &reg,
        stringStart,
        stringEnd,
        onigOptions,
        ONIG_ENCODING_UTF16_LE,
        ONIG_SYNTAX_DEFAULT,
        &einfo);

	return reg;
}

void onigwrap_region_free(OnigRegion *region)	
{
	onig_region_free(region, 1);
}

void onigwrap_free(regex_t *reg)
{
	onig_free(reg);
}

int onigwrap_index_in(regex_t *reg, char *charPtr, int offset, int length)
{
	OnigUChar *stringStart  = (OnigUChar*) charPtr;
	OnigUChar *stringEnd    = (OnigUChar*) (charPtr + length);
	OnigUChar *stringOffset = (OnigUChar*) (charPtr + offset);
	OnigUChar *stringRange  = (OnigUChar*) stringEnd;

	OnigRegion *region = onig_region_new();
	int result = onig_search(
        reg,
        stringStart,
        stringEnd,
        stringOffset,
        stringRange,
        region,
        ONIG_OPTION_NONE);

	onig_region_free(region, 1);

	if (result >= 0)
		return result >> 1;
	if (result == ONIG_MISMATCH)
		return -1;
	return -2;
}

OnigRegion *onigwrap_search(regex_t *reg, char *charPtr, int offset, int length)
{
	OnigUChar *stringStart  = (OnigUChar*) charPtr;
	OnigUChar *stringEnd    = (OnigUChar*) (charPtr + length);
	OnigUChar *stringOffset = (OnigUChar*) (charPtr + offset);
	OnigUChar *stringRange  = (OnigUChar*) stringEnd;

	OnigRegion *region = onig_region_new();

	int result = onig_search(reg, stringStart, stringEnd, stringOffset, stringRange, region, ONIG_OPTION_NONE);
	return region;
}

int onigwrap_num_regs(OnigRegion *region)
{
	return region->num_regs;
}

int onigwrap_pos(OnigRegion *region, int nth)
{
	if (nth < region->num_regs)
	{
		int result = region->beg[nth];
		if (result < 0)
			return result;
		return result >> 1;
	}
	return -3;
}

int onigwrap_len(OnigRegion *region, int nth)
{
	if (nth < region->num_regs)
	{
		int result = region->end[nth] - region->beg[nth];
		return result >> 1;
	}
	return -4;
}
