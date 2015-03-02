/**
 * Update: CR-002
 * Applies to: 1.0.0.0 
 * Notes:
 *	- Add performance indexes to key merge tables
 *	- Order merge candidates
 */

-- MERGE CANDIDATE PATCHES
create index hsr_mrg_cand_hsr_id_idx on hsr_mrg_cand_tbl(hsr_id);
create index hsr_mrg_cand_cand_hsr_id_idx on hsr_mrg_cand_tbl(cand_hsr_id);
CREATE OR REPLACE FUNCTION get_outsd_mrg()
  RETURNS TABLE(hsr_id numeric, efft_vrsn_id numeric) AS
$BODY$
BEGIN
	RETURN QUERY SELECT DISTINCT A.HSR_ID, A.EFFT_VRSN_ID FROM HSR_MRG_CAND_TBL A WHERE OBSLT_UTC IS NULL ORDER BY A.EFFT_VRSN_ID DESC;
END;
$BODY$
  LANGUAGE plpgsql;